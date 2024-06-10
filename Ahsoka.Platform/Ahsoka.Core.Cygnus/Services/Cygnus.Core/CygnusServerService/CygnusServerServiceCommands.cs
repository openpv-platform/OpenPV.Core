using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Cygnus.Reach;

namespace Ahsoka.Services.Cygnus;

// TODO: BMG (Nov. 03, 2023) Create the underlying service base that is going to be used for this class.
/// <summary>
/// 
/// </summary>
public partial class CygnusServerService
{
    /// <summary>
    /// The File data service origin name for commands that are going to be exposed through Cygnus
    /// </summary>
    public const string ReachConfigCommands = "Commands" + ReachConfigSeparator;
    /// <summary>
    /// The data service value key string that is going to be used for moving data between the cygnus server and the
    /// application.
    /// </summary>
    public const string ReachCommandData = "CommandData";

    private KeyList CommandValueKeys;

    private KeyInfo DataServiceCommandDataKeyInfo;
    private KeyValueList DataServiceCommandDataKeyValueList = new();

    private void GetCommandValueKeys()
    {
        // Find all the data service values that have an origin indicating that they are commands that are going to
        // be exposed through the cygnus commands api.
        AvailableKeySearchInfo searchInfo = new()
        {
            OriginFilter = CygnusService.Name,
            KeyFilter = ReachConfigCommands,
        };
        CommandValueKeys = DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: DataServiceTimeout);

        if (CommandValueKeys.Keys.Count == 0)
            return;

        servicesAvailable |= (UInt32)ServiceIds.Commands;

        searchInfo.KeyFilter = ReachCommandData;
        DataServiceCommandDataKeyInfo = DataClient.SearchAvailableKeys(searchInfo).Keys[0];
        // We have to reset the publisher ID to null to avoid our set values looking like they're coming from the
        // original publisher.
        DataServiceCommandDataKeyInfo.PublisherId = null;
        DataServiceCommandDataKeyValueList.Values.Add(new() { KeyInfo = DataServiceCommandDataKeyInfo });

        KeySubscriptionRequest keySubRequest = new();
        keySubRequest.SubscribeToKeys.Add(new() { ValueId = DataServiceCommandDataKeyInfo.ValueId });
        DataClient.SubscribeToKeys(keySubRequest);

    }

    private DiscoverCommandsResult ProcessDiscoverCommands(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, DiscoverCommands request)
    {
        // SendMessageInternal(messageHeader, new DiscoverCommandsResult());

        List<DiscoverCommandsResult> responseList = new();

        for (int i = 0; i < CommandValueKeys.Keys.Count; i++)
        {
            DiscoverCommandsResult responseObject = new();
            CommandInfo commandInfo;

            // for (; responseObject.AvailableCommands.Count < CygnusBufferSizes.NumCommandsInResponse && i < CommandValueKeys.Keys.Count; i++)
            for (; i < CommandValueKeys.Keys.Count; i++)
            {
                KeyList getKeyValueList = new();
                getKeyValueList.Keys.Add(CommandValueKeys.Keys[i]);
                KeyValueList valueList = DataClient.GetKeyValues(getKeyValueList);
                byte[] commandInfoByteArray = valueList.Values[0].DataValue;

                using MemoryStream commandInfoStream = new(commandInfoByteArray);
                commandInfo = new();
                ProtoBuf.Serializer.Deserialize(commandInfoStream, commandInfo);

                responseObject.AvailableCommands.Add(commandInfo);
            }

            responseList.Add(responseObject);
        }

        // SendMessageArrayInternal(messageHeader, responseList.ToArray());
        return responseList[0];
    }

    // TODO: BMG (Jan. 19, 2024) While this current implementation is architected to handle a single command at a time,
    // this could definitely be updated later down the road to support multiple commands being run simultaneously.
    private SendCommandResult ProcessSendCommand(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, SendCommand request)
    {
        using MemoryStream sendCommandStream = new();
        ProtoBuf.Serializer.Serialize(sendCommandStream, request);
        DataServiceCommandDataKeyValueList.Values[0].DataValue = sendCommandStream.ToArray();

        ManualResetEventSlim mres = new();
        SendCommandResult sendCommandResult = new();
        var SendCommandReturnHandler =
            new EventHandler<DataServiceClient.AhsokaNotificationArgs>(
                (object sender, DataServiceClient.AhsokaNotificationArgs notificationInfo) =>
                {
                    if (notificationInfo.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
                    {
                        if (DataServiceCommandDataKeyInfo == null)
                            return;

                        KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;
                        int valueIndex = notif.KeyValues.FindIndex(x => x.KeyInfo.ValueId == DataServiceCommandDataKeyInfo.ValueId);
                        if (valueIndex == -1)
                            return;

                        using MemoryStream sendCommandStream = new(notif.KeyValues[valueIndex].DataValue);
                        ProtoBuf.Serializer.Deserialize(sendCommandStream, sendCommandResult);

                        mres.Set();
                    }
                });

        DataClient.NotificationReceived += SendCommandReturnHandler;
        DataClient.SetKeyValues(DataServiceCommandDataKeyValueList);
        mres.Wait();
        DataClient.NotificationReceived -= SendCommandReturnHandler;

        return sendCommandResult;
    }
}
