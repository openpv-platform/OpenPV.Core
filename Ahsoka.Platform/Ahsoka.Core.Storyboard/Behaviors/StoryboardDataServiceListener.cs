using Ahsoka.Services.Data;
using System.Reflection;
using System;
using Ahsoka.Storyboard;
using System.Collections.Generic;
using Ahsoka.ServiceFramework;
using static NetMQ.NetMQSelector;

namespace Ahsoka.Utility;

/// <summary>
/// Behavior / Decorator to Notify the Client with SB Objects 
/// when changes occure to related Keys in the Dataervice
/// </summary>
public class DataServiceListener : IDataServiceBehavior
{
    #region Fields
    Dictionary<string, Tuple<PropertyInfo, Type>> notificationProperties = new();
    StoryboardClient sbClient;
    Type type;
    string originName;
    DataServiceClient dataServiceClient;
    #endregion

    /// <summary>
    /// Constructs the Behavior tied to the specified sbClient.  Thisclient will be notified
    /// on any changes that impact the specified 'subscribeToTypes'.   Note you can also add the types
    /// by using the AddSubscription method.
    /// </summary>
    /// <param name="sbClient">Data Service Client for the Behavior to use</param>
    /// <param name="objectType">Object to Listen For</param>
    /// <param name="dataServiceOriginName">Origin Name to Listen For</param>
    public DataServiceListener(StoryboardClient sbClient, Type objectType, string dataServiceOriginName = null)
    {
        this.sbClient = sbClient;
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
                if (!TryGetSBMessageObject(notif.KeyValues[i], out object sbMessageObject))
                    continue;

                sbClient.QueueMessage(new StoryboardMessage(sbMessageObject));
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
            MininumAccessLevel = System.DataAccessLevel.Public,
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

    bool TryGetSBMessageObject(KeyValue keyValue, out object sbMessageObject)
    {
        string propertyDataServiceKey = $"{keyValue.KeyInfo.Origin}_{keyValue.KeyInfo.Key}";

        if (!notificationProperties.TryGetValue(propertyDataServiceKey, out Tuple<PropertyInfo, Type> propertyInfoAndMessage))
        {
            sbMessageObject = null;
            return false;
        }

        Type sbMessageType = propertyInfoAndMessage.Item2;
        PropertyInfo sbMessagePropertyInfo = propertyInfoAndMessage.Item1;
        sbMessageObject = Activator.CreateInstance(sbMessageType);

        Type sbMessagePropertyType = sbMessagePropertyInfo.PropertyType;
        if (sbMessagePropertyType == typeof(float))
            sbMessagePropertyInfo.SetValue(sbMessageObject, Convert.ToSingle(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(UInt64))
            sbMessagePropertyInfo.SetValue(sbMessageObject, Convert.ToUInt64(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(UInt32))
            sbMessagePropertyInfo.SetValue(sbMessageObject, Convert.ToUInt32(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(UInt16))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            sbMessagePropertyInfo.SetValue(sbMessageObject, (UInt16)tempInt);
        }
        else if (sbMessagePropertyType == typeof(byte))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            sbMessagePropertyInfo.SetValue(sbMessageObject, (byte)tempInt);
        }
        else if (sbMessagePropertyType == typeof(Int64))
            sbMessagePropertyInfo.SetValue(sbMessageObject, Convert.ToInt64(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(Int32))
            sbMessagePropertyInfo.SetValue(sbMessageObject, Convert.ToInt32(keyValue.GetValue()));
        else if (sbMessagePropertyType == typeof(Int16))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            sbMessagePropertyInfo.SetValue(sbMessageObject, (Int16)tempInt);
        }
        else if (sbMessagePropertyType == typeof(sbyte))
        {
            UInt32 tempInt = Convert.ToUInt32(keyValue.GetValue());
            sbMessagePropertyInfo.SetValue(sbMessageObject, (sbyte)tempInt);
        }
        else if (sbMessagePropertyType == typeof(string))
            sbMessagePropertyInfo.SetValue(sbMessageObject, Convert.ToString(keyValue.GetValue()));
        else
            throw new Exception("The value stored in the data service doesn't match up with the " +
                                "storyboard type for some reason. This is a problem.");

        return true;
    }
}
