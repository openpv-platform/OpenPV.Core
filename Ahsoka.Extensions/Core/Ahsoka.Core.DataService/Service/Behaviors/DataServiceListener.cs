using Ahsoka.Dispatch;
using Ahsoka.Services.Data;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Behavior / Decorator to Notify the Client with SB Objects 
/// when changes occure to related Keys in the Dataervice
/// </summary>
public class DataServiceListener : IDataServiceBehavior
{
    #region Fields
    Dictionary<string, Tuple<PropertyInfo, Type>> notificationProperties = new();
    ICanQueue queueClient;
    Type type;
    string originName;
    DataServiceClient dataServiceClient;
    #endregion

    /// <summary>
    /// Constructs the Behavior tied to the specified output queue.  This queue will be enqueued
    /// on any changes that impact the specified 'subscribeToTypes'. 
    /// </summary>
    /// <param name="queueClient">Data Service Client for the Behavior to use</param>
    /// <param name="objectType">Object to Listen For</param>
    /// <param name="dataServiceOriginName">Origin Name to Listen For</param>
    public DataServiceListener(ICanQueue queueClient, Type objectType, string dataServiceOriginName = null)
    {
        this.queueClient = queueClient;
        type = objectType;
        originName = dataServiceOriginName;
    }

    void IDataServiceBehavior.Init(AhsokaClientBase<DataMessageTypes.Ids> dataServiceClient)
    {
        this.dataServiceClient = (DataServiceClient)dataServiceClient;

        ListenForType();
    }

    void IDataServiceBehavior.OnDataServiceNotificationReceived(DataMessageTypes.Ids transportId, object requestObject)
    {
        if (transportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
        {
            KeyValueNotification notif = requestObject as KeyValueNotification;
            for (int i = 0; i < notif.KeyValues.Count; i++)
            {
                if (!TryGetViewModelObject(notif.KeyValues[i], out object messageObject))
                    continue;

                queueClient.Enqueue(messageObject);
            }
        }
    }

    /// <summary>
    /// Add Subscriptions for the Specfied Type.  This will send notifiation 
    /// objects to the Client when changes occur in the Data Service.
    /// </summary>
    void ListenForType()
    {
        if (originName == null) 
            originName = type.Name;

        foreach (var item in GetType().GetProperties())
        {
            var attribute = item.GetCustomAttribute<ViewModelPropertyBaseAttribute>();
            if (attribute != null)
            {
                // Generate Subscription to this Key / Value Pair
                KeyValue keyValue = GenerateKeyValue(item.GetValue(this), originName, item.Name);
                OnSubscribeDataServiceValue(dataServiceClient, keyValue.KeyInfo);

                // Add Key / Value to Dictionary
                string propertyName = $"{originName}_{item.Name}";
                notificationProperties.Add(propertyName, new(item, type));
            }
        }
    }

    private KeyValue GenerateKeyValue(object newValue, string originaName, string keyString)
    {
        KeyInfo keyInfo = new()
        {
            Key = keyString,
            Origin = originaName,
            MininumAccessLevel = DataAccessLevel.Public,
        };
        KeyValue keyValue = new()
        {
            KeyInfo = keyInfo,
        };

        keyValue.SetValue(newValue);

        return keyValue;
    }



    /// <summary>
    /// Subscribes to a given value.
    /// </summary>
    /// <param name="client">Data Service Client.</param>
    /// <param name="keyInfo">The key info of the data service value that we are going to be subscribed to.</param>
    protected virtual void OnSubscribeDataServiceValue(DataServiceClient client, KeyInfo keyInfo)
    {
        KeySubscriptionRequest subRequest = new();
        subRequest.SubscribeToKeys.Add(keyInfo);
        client.SubscribeToKeys(subRequest);
    }

    bool TryGetViewModelObject(KeyValue keyValue, out object messageObject)
    {
        string propertyDataServiceKey = $"{keyValue.KeyInfo.Origin}_{keyValue.KeyInfo.Key}";

        if (!notificationProperties.TryGetValue(propertyDataServiceKey, out Tuple<PropertyInfo, Type> propertyInfoAndMessage))
        {
            messageObject = null;
            return false;
        }

        Type sbMessageType = propertyInfoAndMessage.Item2;
        PropertyInfo messagePropertyInfo = propertyInfoAndMessage.Item1;
        messageObject = Activator.CreateInstance(sbMessageType);

        Type sbMessagePropertyType = messagePropertyInfo.PropertyType;
        if (sbMessagePropertyType == typeof(float))
            messagePropertyInfo.SetValue(messageObject, Convert.ToSingle(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(UInt64))
            messagePropertyInfo.SetValue(messageObject, Convert.ToUInt64(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(UInt32))
            messagePropertyInfo.SetValue(messageObject, Convert.ToUInt32(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(UInt16))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            messagePropertyInfo.SetValue(messageObject, (UInt16)tempInt);
        }
        else if (sbMessagePropertyType == typeof(byte))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            messagePropertyInfo.SetValue(messageObject, (byte)tempInt);
        }
        else if (sbMessagePropertyType == typeof(Int64))
            messagePropertyInfo.SetValue(messageObject, Convert.ToInt64(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(Int32))
            messagePropertyInfo.SetValue(messageObject, Convert.ToInt32(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(Int16))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            messagePropertyInfo.SetValue(messageObject, (Int16)tempInt);
        }
        else if (sbMessagePropertyType == typeof(sbyte))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            messagePropertyInfo.SetValue(messageObject, (sbyte)tempInt);
        }
        else if (sbMessagePropertyType == typeof(string))
            messagePropertyInfo.SetValue(messageObject, Convert.ToString(keyValue.GetValue()));
        else
            throw new Exception("The value stored in the data service doesn't match up with the " +
                                "storyboard type for some reason. This is a problem.");

        return true;
    }
}
