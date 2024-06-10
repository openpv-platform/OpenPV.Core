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
    /// The data service value key string that is going to be used for moving data between the cygnus server and the
    /// application.
    /// </summary>
    public const string ReachCLIData = "CLIData";

    private KeyInfo DataServiceCLIDataKeyInfo;
    private KeyValueList DataServiceCLIDataKeyValueList = new();

    private void GetCLIValueKeys()
    {
        // Find all the data service values that have an origin indicating that they are commands that are going to
        // be exposed through the cygnus commands api.
        AvailableKeySearchInfo searchInfo = new()
        {
            OriginFilter = CygnusService.Name,
            KeyFilter = ReachCLIData,
        };
        KeyList availableKeys = DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: DataServiceTimeout);

        if (availableKeys.Keys.Count == 0)
            return;

        DataServiceCLIDataKeyInfo = availableKeys.Keys[0];
        // We have to reset the publisher ID to null to avoid our set values looking like they're coming from the
        // original publisher.
        DataServiceCLIDataKeyInfo.PublisherId = null;

        DataServiceCLIDataKeyValueList.Values.Add(new() { KeyInfo = DataServiceCLIDataKeyInfo });

        DataClient.NotificationReceived += ProcessCliDataReturnHandler;
        KeySubscriptionRequest keySubRequest = new();
        keySubRequest.SubscribeToKeys.Add(new() { ValueId = DataServiceCLIDataKeyInfo.ValueId });
        DataClient.SubscribeToKeys(keySubRequest);

        servicesAvailable |= (UInt32)ServiceIds.Cli;
    }

    private void SendCLIDataNotification(CLIData cliDataReceived, CygnusInterface? omitInterfaceType = null)
    {
        foreach (var keyval in OutgoingEndpoints)
        {
            if (omitInterfaceType != null && keyval.Key == omitInterfaceType)
                continue;

            if (keyval.Value.CLIHeader != null)
                keyval.Value.Endpoint.SendNotification(keyval.Value.CLIHeader, cliDataReceived);
        }
    }

    private void ProcessCliDataReturnHandler(object sender, DataServiceClient.AhsokaNotificationArgs notificationInfo)
    {
        if (notificationInfo.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
        {
            if (DataServiceCLIDataKeyInfo == null)
                return;

            KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;
            int valueIndex = notif.KeyValues.FindIndex(x => x.KeyInfo.ValueId == DataServiceCLIDataKeyInfo.ValueId);
            if (valueIndex == -1)
                return;

            CLIData cliDataReceived = new()
            {
                MessageData = notif.KeyValues[valueIndex].StringValue,
            };

            SendCLIDataNotification(cliDataReceived);
        }
    }

    private void ProcessCliData(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, CLIData request)
    {
        OutgoingEndpoints[interfaceType].CLIHeader = messageHeader;

        DataServiceCLIDataKeyValueList.Values[0].StringValue = request.MessageData;
        DataClient.SetKeyValues(DataServiceCLIDataKeyValueList);

        // NOTE: BMG (Jan. 26, 2024) This isn't technically in the specification, but if there are other clients
        // connected to this server through other interfaces, they should receive all the data to and from the CLI.
        SendCLIDataNotification(request, interfaceType);
    }
}
