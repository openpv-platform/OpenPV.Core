using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.Services.Data;

/// <summary>
/// Global Data Service Client For Service to Service Data Sharing
/// </summary>
public class DataServiceClient : AhsokaClientBase<DataMessageTypes.Ids> 
{
    /// <summary>
    /// The subscriber ID that has been generated for this client.
    /// </summary>
    public List<string> SubscriberIds = new(){ };

    /// <summary>
    /// Default Constructor that uses the System Default Configuration
    /// </summary>
    public DataServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(DataService.Name))
    {

    }

    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// </summary>
    /// <param name="config"></param>
    public DataServiceClient(ServiceConfiguration config) :
        base(config, new DataServiceMessages())
    {
        SubscriberIds.Add(this.GetEndPointId().ToGuidString());
    }

    /// <summary>
    /// The function defining how the client is going to handle the notifications that are received from the socket.
    /// </summary>
    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<DataMessageTypes.Ids>())
        {
            case DataMessageTypes.Ids.NotifyKeyValueChanged:
                KeyValueNotification notif = message as KeyValueNotification;
                // If the subscriber ID of the notification doesn't match the subscriber ID that was generated for this
                // client then we aren't going to invoke the event for received events. This only affects the
                // KeyValueChanged events.
                if (!SubscriberIds.Contains(notif.SubscriberId))
                    return;
                break;

            default:
                break;
        }

        base.OnHandleReceive(messageHeader, message);
    }

    /// <summary>
    /// Create a Data Service Object
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new DataService(this.ServiceConfig);
    }

    /// <summary>
    /// Starts the Service to manage the DataService Cache for all Other.
    /// This is only needed if you are not using a dispatcher and relying on auto start.
    /// </summary>
    public void EnableCache(int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse(DataMessageTypes.Ids.EnableCache, timeoutInMs);
    }

    /// <summary>
    /// Gets a list of Keys available in the Service
    /// </summary>
    /// <param name="searchInfo"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    public KeyList SearchAvailableKeys(AvailableKeySearchInfo searchInfo = null, int timeoutInMs = int.MaxValue)
    {
        searchInfo ??= new AvailableKeySearchInfo();
     
        return SendNotificationWithResponse<KeyList>(DataMessageTypes.Ids.SearchAvailableKeys, searchInfo, timeoutInMs);
    }

    /// <summary>
    /// Subscribe to a set of Keys
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    public KeySubscriberInfo SubscribeToKeys(KeySubscriptionRequest subscription, int timeoutInMs = int.MaxValue)
    {
        return SendNotificationWithResponse<KeySubscriberInfo>(DataMessageTypes.Ids.SubscribeToKeys, subscription, timeoutInMs);
    }

    /// <summary>
    ///  Unsubscribe to a set of Keys
    /// </summary>
    /// <param name="subscription">Topic to Unsubscribe</param>
    /// <param name="timeoutInMs"></param>
    public void UnsubscribeToKeys(KeySubscriptionRequest subscription, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse(DataMessageTypes.Ids.UnsubscribeToKeys, subscription, timeoutInMs);
    }

    /// <summary>
    /// Set Key Values
    /// </summary>
    /// <param name="keyValues"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    public void SetKeyValues(KeyValueList keyValues, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse(DataMessageTypes.Ids.SetKeyValues, keyValues, timeoutInMs);
    }

    /// <summary>
    /// Get Key Values
    /// </summary>
    /// <param name="message"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    public KeyValueList GetKeyValues(KeyList message, int timeoutInMs = int.MaxValue)
    {
        return SendNotificationWithResponse<KeyValueList>(DataMessageTypes.Ids.GetKeyValues, message, timeoutInMs);
    }

    /// <summary>
    /// Register a publisher Id to listen for in notifications
    /// </summary>
    /// <param name="publisherId"></param>
    /// <returns></returns>
    public void RegisterPublisherId(string publisherId)
    {
        SubscriberIds.Add(publisherId);
    }
}


/// <summary>
/// List of Keys available in the Data Service
/// </summary>
public partial class KeyList
{
    /// <summary>
    /// Get a Key from the Key List
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public KeyInfo GetKeyInfo(string origin, string key)
    {
        return this.Keys.Find(x => x.Origin == origin && x.Key == key);
    }
}

/// <summary>
/// Attribute for Adorning KnownKeyObjects
/// </summary>
public class AhsokaServiceDataValues : Attribute
{
    /// <summary>
    /// Associated Service
    /// </summary>
    public string ServiceName { get; init; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceName"></param>
    public AhsokaServiceDataValues(string serviceName) { ServiceName = serviceName; }
}