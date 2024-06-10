using System;
using System.Collections.Generic;
using System.IO;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.System;
using Cygnus.Reach;

namespace Ahsoka.Services.Cygnus;

/// <summary>
/// Client for accessing Ahsoka Bluetooth LE Service
/// </summary>
public class CygnusServiceClient : AhsokaClientBase<CygnusMessageTypes.Ids>
{
    private const string CygnusDataServiceCommandDataSubscriberUUID = "f680069c-da9d-4342-80da-43dffc6af331";

    /// <summary>
    /// A callback function delegate for handling cygnus command requests.
    /// </summary>
    public delegate int CygnusCommandCallback(out string message);
    /// <summary>
    /// A callback function delegate for handling incoming CLI input.
    /// </summary>
    public delegate string CygnusCLICallback(string input);
    private Dictionary<uint, CygnusCommandCallback> callbackDictionary = new();
    private DataServiceClient CommandDataClient;
    private int DataServiceCommandDataKeyId;
    private DataServiceClient CliDataClient;
    private CygnusCLICallback CLICallback;
    private int DataServiceCLIDataKeyId;

    /// <summary>
    /// Default Constructor
    /// </summary>
    public CygnusServiceClient()
        : this(ConfigurationLoader.GetServiceConfig(CygnusService.Name))
    {

    }

    /// <summary>
    /// Constructor for accepting customer configurations
    /// </summary>
    /// <param name="config"></param>
    public CygnusServiceClient(ServiceConfiguration config) :
        base(config, new CygnusServiceMessage())
    {

    }

    /// <summary>
    /// Creates a Default Service Instance for In Proc and
    /// automatic service construction.
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new CygnusService(ServiceConfig);
    }

    /// <summary>
    /// Enable the transport to access the Cygnus Server.
    ///
    /// NOTE: It is important to keep in mind that once the transport is enabled, we are going to freeze all the values
    /// that are going to be exposed through Cygnus. This includes any parameters in the data service, any files that
    /// have been added, any commands available, and any streams.
    /// </summary>
    /// <param name="interfaceType"></param>
    public void EnableTransport(CygnusInterface interfaceType)
    {
        CygnusServiceEnableDisableTransports enableTransports = new();
        enableTransports.Interface = interfaceType;

        SendNotificationWithResponse<EmptyNotification>(CygnusMessageTypes.Ids.EnableTransport, enableTransports);
    }

    /// <summary>
    /// Add a file to the set of files that are going to be exposed through cygnus.
    /// </summary>
    /// <param name="fileInfo"></param>
    public void AddFile(CygnusServiceFileInfo fileInfo)
    {
        SendNotificationWithResponse<EmptyNotification>(CygnusMessageTypes.Ids.AddFile, fileInfo);
    }

    /// <summary>
    /// Add a file to the set of files that are going to be exposed through cygnus.
    /// </summary>
    /// <param name="commandInfo"></param>
    public CygnusServiceCommandInfoResponse AddCommand(CygnusServiceCommandInfo commandInfo)
    {
        return SendNotificationWithResponse<CygnusServiceCommandInfoResponse>(CygnusMessageTypes.Ids.AddCommand, commandInfo);
    }

    /// <summary>
    /// Add a file to the set of files that are going to be exposed through cygnus.
    /// </summary>
    public CygnusServiceEnableCLIResponse EnableCLI()
    {
        return SendNotificationWithResponse<CygnusServiceEnableCLIResponse>(CygnusMessageTypes.Ids.EnableCli);
    }

    private void CygnusCommandCallbackEventHandler(object sender, DataServiceClient.AhsokaNotificationArgs notificationInfo)
    {
        if (notificationInfo.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
        {
            if (DataServiceCommandDataKeyId <= 0)
                return;

            KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;

            // We're going to get the values that were updated, find the one that corresponds to the command data, and
            // deserialize that into the SendCommand that is received.
            int valueIndex = notif.KeyValues.FindIndex(x => x.KeyInfo.ValueId == DataServiceCommandDataKeyId);
            if (valueIndex == -1)
                return;
            if (notif.KeyValues[valueIndex].KeyInfo.PublisherId == CygnusDataServiceCommandDataSubscriberUUID)
                return;

            using MemoryStream sendCommandStream = new(notif.KeyValues[valueIndex].DataValue);
            SendCommand sendCommandReceived = new();
            ProtoBuf.Serializer.Deserialize(sendCommandStream, sendCommandReceived);

            if (callbackDictionary.TryGetValue(sendCommandReceived.CommandId, out CygnusCommandCallback cb))
            {
                KeyValue keyValue = new();
                keyValue.KeyInfo = notif.KeyValues[valueIndex].KeyInfo;
                keyValue.KeyInfo.PublisherId = CygnusDataServiceCommandDataSubscriberUUID;
                SendCommandResult resultData = new();
                resultData.Result = cb.Invoke(out string resultMessage);
                resultData.ResultMessage = resultMessage;
                using MemoryStream resultDataStream = new();
                ProtoBuf.Serializer.Serialize(resultDataStream, resultData);
                keyValue.DataValue = resultDataStream.ToArray();

                KeyValueList keyValueList = new();
                keyValueList.Values.Add(keyValue);
                CommandDataClient.SetKeyValues(keyValueList);
            }
        }
    }

    /// <summary>
    /// Add a file to the set of files that are going to be exposed through cygnus.
    /// </summary>
    /// <param name="commandInfo">The reach command info that describes the command.</param>
    /// <param name="dataClient">The data client for the application.</param>
    /// <param name="cb">The callback function that corresponds to the given command.</param>
    public void RegsiterCommandCallback(CygnusServiceCommandInfo commandInfo, DataServiceClient dataClient, CygnusCommandCallback cb)
    {
        CygnusServiceCommandInfoResponse addCommandResponse = AddCommand(commandInfo);
        if (DataServiceCommandDataKeyId <= 0)
        {
            DataServiceCommandDataKeyId = addCommandResponse.DataKeyId;
            CommandDataClient = dataClient;
            CommandDataClient.NotificationReceived += CygnusCommandCallbackEventHandler;

            KeySubscriptionRequest keySubRequest = new() { SubscriberId = CygnusDataServiceCommandDataSubscriberUUID };
            keySubRequest.SubscribeToKeys.Add(new() { ValueId = DataServiceCommandDataKeyId });
            CommandDataClient.RegisterPublisherId(CygnusDataServiceCommandDataSubscriberUUID);
            CommandDataClient.SubscribeToKeys(keySubRequest);
        }
        callbackDictionary.Add(addCommandResponse.CommandInfo.Id, cb);
    }

    private void CygnusCLICallbackEventHandler(object sender, DataServiceClient.AhsokaNotificationArgs notificationInfo)
    {
        if (notificationInfo.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
        {
            if (DataServiceCLIDataKeyId <= 0)
                return;

            KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;
            int valueIndex = notif.KeyValues.FindIndex(x => x.KeyInfo.ValueId == DataServiceCLIDataKeyId);
            if (valueIndex == -1)
                return;

            KeyValue keyValue = new();
            keyValue.KeyInfo = notif.KeyValues[valueIndex].KeyInfo;
            keyValue.KeyInfo.PublisherId = null;
            keyValue.StringValue = CLICallback.Invoke(notif.KeyValues[valueIndex].StringValue);
            KeyValueList keyValueList = new();
            keyValueList.Values.Add(keyValue);
            CliDataClient.SetKeyValues(keyValueList);
        }
    }

    /// <summary>
    /// Add a file to the set of files that are going to be exposed through cygnus.
    /// </summary>
    /// <param name="cliCallback">The callback function that corresponds to the given command.</param>
    /// <param name="dataClient">The callback function that corresponds to the given command.</param>
    public void RegisterCLICallback(DataServiceClient dataClient, CygnusCLICallback cliCallback)
    {
        if (CLICallback != null)
            throw new Exception("A CLI has already been registered by this application.");

        CygnusServiceEnableCLIResponse enableCLIResponse = EnableCLI();
        if (enableCLIResponse.DataKeyId == -1)
            throw new Exception("A CLI has already been registered by another application.");

        if (CliDataClient == null)
            CliDataClient = dataClient;

        CliDataClient.NotificationReceived += CygnusCLICallbackEventHandler;
        DataServiceCLIDataKeyId = enableCLIResponse.DataKeyId;
        CLICallback = cliCallback;
        KeySubscriptionRequest keySubRequest = new();
        keySubRequest.SubscribeToKeys.Add(new() { ValueId = enableCLIResponse.DataKeyId });
        CliDataClient.SubscribeToKeys(keySubRequest);
    }
}
