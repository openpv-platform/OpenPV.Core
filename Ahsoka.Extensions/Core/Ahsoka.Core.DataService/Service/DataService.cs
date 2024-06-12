#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.Services.Data;

/// <summary>
/// Basic Implementation of a Service Cache
/// </summary>
[AhsokaService(DataService.Name)]
public class DataService : AhsokaServiceBase<DataMessageTypes.Ids>,IServiceCache
{
    #region Fields
    readonly static object syncRoot = new();
    public const string Name = "DataService";
    readonly Dictionary<string, Dictionary<string, KeyInfo>> keyCache = new(); // Stores Key Info 
    readonly List<ValueItem> valueCache = new(); // Stores Values
    #endregion

    #region Methods
    public DataService() : 
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
       
    }

    public DataService(ServiceConfiguration config) :
        base(config, new DataServiceMessages())
    {
        IServiceCache.Default = this;

        this.UpdateCacheValue("Available", "true");
    }

    /// <summary>
    /// Internal Function used by Services to Send Data to the Data Service
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="publisherId"></param>
    int IServiceCache.UpdateCacheValue(string origin, string key, object value, string publisherId)
    {
        lock (syncRoot)
        {
            var notifications = new NotificationList();
            int valueID = CreateKeyIfNotExists(new KeyInfo() { Origin = origin, Key = key, PublisherId = publisherId });
            KeyInfo returnKeyInfo = UpdateValue(valueID, value, notifications, publisherId);

            SendNotifications(notifications);
            return valueID;
        }
    }

    protected override void OnClientConnected()
    {
        base.OnClientConnected();

        // Notify Clients of any new keys
        var list = keyCache.Values.SelectMany(x=>x.Values).ToList();
        NotifyKeysAdded(list.ToArray());
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        // We are only protecting Inter Service Calls since this service only 
        // runs a single request at a time
        lock (syncRoot)
        {
            switch (messageHeader.TransportId.IntToEnum<DataMessageTypes.Ids>())
            {
                case DataMessageTypes.Ids.EnableCache:
                    HandleEnableCache(messageHeader);
                    break;

                case DataMessageTypes.Ids.SearchAvailableKeys:
                    HandleGetAvailableKeys(messageHeader, message as AvailableKeySearchInfo);
                    break;

                case DataMessageTypes.Ids.GetKeyValues:
                    HandleGetKeyValues(messageHeader, message as KeyList);
                    break;

                case DataMessageTypes.Ids.SetKeyValues:
                    HandleSetKeyValues(messageHeader, message as KeyValueList);
                    break;

                case DataMessageTypes.Ids.SubscribeToKeys:
                    HandleSubscribeToKeys(messageHeader, message as KeySubscriptionRequest);
                    break;

                case DataMessageTypes.Ids.UnsubscribeToKeys:
                    HandleUnSubscribeToKeys(messageHeader, message as KeySubscriptionRequest);
                    break;
            }
        }
    }

    /// <summary>
    /// Note that this is effectively an "Echo" call so that the Service is custructed
    /// and added as the "Default" cache.  
    /// </summary>
    /// <param name="messageHeader"></param>
    private void HandleEnableCache(AhsokaMessageHeader messageHeader)
    {
        SendMessageInternal(messageHeader);
    }

    private void HandleGetAvailableKeys(AhsokaMessageHeader messageHeader, AvailableKeySearchInfo availableKeySearchInfo)
    {
        KeyList list = new();
        bool match;
        foreach (var item in keyCache)
        {
            foreach (var value in item.Value.Values)
            {
                match = value.Origin.StartsWith(availableKeySearchInfo.OriginFilter) && 
                    value.Key.StartsWith(availableKeySearchInfo.KeyFilter) && 
                    value.MininumAccessLevel >= availableKeySearchInfo.MininumAccessLevel;

                if (availableKeySearchInfo.InvertFilters)
                    match = !match;

                if (match)
                {
                    if (!String.IsNullOrEmpty(availableKeySearchInfo.SubscriberIdFilter))
                    {
                        if (valueCache[value.ValueId].Subscribers != null && 
                            valueCache[value.ValueId].Subscribers.Contains(availableKeySearchInfo.SubscriberIdFilter))
                            list.Keys.Add(value);
                    }
                    else
                        list.Keys.Add(value);
                }
            }
        }

        SendMessageInternal(messageHeader, list);
    }

    private void HandleSetKeyValues(AhsokaMessageHeader messageHeader, KeyValueList keyValueList)
    {
        var notificationList  = new NotificationList();

        foreach (var keyValue in keyValueList.Values)
        {
            if (String.IsNullOrEmpty(keyValue.KeyInfo.PublisherId))
                keyValue.KeyInfo.PublisherId = messageHeader.ClientId.ToGuidString();

            if (keyValue.KeyInfo.ValueId <= 0)
            {
                int valueID = CreateKeyIfNotExists(keyValue.KeyInfo);
                UpdateValue(valueID, keyValue.GetValue(), notificationList, keyValue.KeyInfo.PublisherId);
            }
            else
                UpdateValue(keyValue.KeyInfo.ValueId, keyValue.GetValue(), notificationList, keyValue.KeyInfo.PublisherId);
        }
        SendMessageInternal(messageHeader);

        SendNotifications(notificationList);
    }
    
    void HandleGetKeyValues(AhsokaMessageHeader messageHeader, KeyList keyList)
    {
        KeyValueList valuesList = new();
        foreach(var keyInfo in keyList.Keys)
        {
            KeyValue kv = new() { KeyInfo = keyInfo };
            
            int valueID = keyInfo.ValueId;
            if (keyInfo.ValueId <= 0)
                valueID = CreateKeyIfNotExists(keyInfo);

           kv.SetValue(valueCache[valueID].Value);

            valuesList.Values.Add(kv);
        }

        SendMessageInternal(messageHeader, valuesList);
    }

    private void HandleSubscribeToKeys(AhsokaMessageHeader messageHeader, KeySubscriptionRequest keySubscriptionRequest)
    {
        // Assign a Key if Not Available
        if (String.IsNullOrEmpty(keySubscriptionRequest.SubscriberId))
            keySubscriptionRequest.SubscriberId = messageHeader.ClientId.ToGuidString();

        // Check if the request to become provider is valid or not.
        foreach (var subscriptionkey in keySubscriptionRequest.SubscribeToKeys)
        {
            int valueID = subscriptionkey.ValueId;
            if (subscriptionkey.ValueId <= 0)
                valueID = CreateKeyIfNotExists(subscriptionkey);

            var valueItem = valueCache[valueID];

            // Add Subscriptions
            if (valueItem.Subscribers == null)
                valueItem.Subscribers = new List<string>() { keySubscriptionRequest.SubscriberId };
            else if (!valueItem.Subscribers.Contains(keySubscriptionRequest.SubscriberId))
                valueItem.Subscribers.Add(keySubscriptionRequest.SubscriberId);
        }

        SendMessageInternal(messageHeader, new KeySubscriberInfo() { SubscriberId = keySubscriptionRequest.SubscriberId });
    }

    private void HandleUnSubscribeToKeys(AhsokaMessageHeader messageHeader, KeySubscriptionRequest keySubscriptionRequest)
    {
        foreach (var subscriptionkey in keySubscriptionRequest.SubscribeToKeys)
        {
            int valueID = subscriptionkey.ValueId;
            if (subscriptionkey.ValueId <= 0)
                valueID = CreateKeyIfNotExists(subscriptionkey);

            var valueItem = valueCache[valueID];

            // Remove and Clear Subscription List if empty
            valueItem.Subscribers?.Remove(keySubscriptionRequest.SubscriberId);
            if (valueItem.Subscribers?.Count == 0)
                valueItem.Subscribers = null;
        }

        SendMessageInternal(messageHeader);
    }

    #endregion

    #region Value Management Methods
    private void SendNotifications(NotificationList notification)
    {
        foreach (var item in notification.NotificationsToSend)
            SendMessageInternal(new AhsokaMessageHeader { TransportId = DataMessageTypes.Ids.NotifyKeyValueChanged.EnumToInt() }, item.Value, true);
    }

    private KeyInfo UpdateValue(int valueID, object value, NotificationList notificationList, string publisherId)
    {
        if (valueCache[valueID].Value != value)
        {
            // Update Cache Value
            valueCache[valueID].Value = value;

            // Add Notifications
            notificationList.AddNotification(valueCache[valueID], publisherId);
        }

        return valueCache[valueID].KeyInfo;
    }

    private int CreateKeyIfNotExists(KeyInfo keyInfo)
    {
        if (!keyCache.TryGetValue(keyInfo.Origin, out Dictionary<string, KeyInfo> keyMap))
        {
            keyMap = new Dictionary<string, KeyInfo>();
            keyCache.Add(keyInfo.Origin, keyMap);
        }

        if (keyMap.TryGetValue(keyInfo.Key, out KeyInfo info))
            return info.ValueId;

        // Create Key / Value ID and add to Key Map
        keyInfo.ValueId = valueCache.Count;

        keyMap.Add(keyInfo.Key, keyInfo);
        valueCache.Add(new ValueItem(keyInfo));

        // Notify Clients that new keys are availble.
        if (IsRunning)
            NotifyKeysAdded(keyInfo);

        return keyInfo.ValueId;
    }

    private void NotifyKeysAdded(params KeyInfo[] keyInfo)
    {
        var header = new AhsokaMessageHeader() { TransportId = DataMessageTypes.Ids.NotifyKeyAdded.EnumToInt() };
        var keyList = new KeyList();
        keyList.Keys.AddRange(keyInfo);
        SendMessageInternal(header, keyList, true);
    }

  
    #endregion

    class ValueItem
    {
        public object Value;
        public List<string> Subscribers;
        public KeyInfo KeyInfo;
        public ValueItem(KeyInfo keyInfo)
        {
            KeyInfo = keyInfo;
        }
    }

    class NotificationList
    {
        public Dictionary<string, KeyValueNotification> NotificationsToSend = new();

        internal void AddNotification(ValueItem valueItem, string publisherId)
        {
            if (valueItem.Subscribers != null)
                foreach (var subscriber in valueItem.Subscribers)
                {
                    if (subscriber.Equals(publisherId))
                        continue;

                    if (!NotificationsToSend.TryGetValue(subscriber, out KeyValueNotification notif))
                    {
                        notif = new KeyValueNotification();
                        NotificationsToSend.Add(subscriber, notif);
                    }

                    // Add Notification Value
                    var val = new KeyValue() { KeyInfo = valueItem.KeyInfo };
                    val.SetValue(valueItem.Value);
                    notif.KeyValues.Add(val);
                    notif.SubscriberId = subscriber;
                }
        }
    }
}
