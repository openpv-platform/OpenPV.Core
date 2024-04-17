using Ahsoka.Services.Data;
using System.Reflection;
using System;

namespace Ahsoka.Utility;

/// <summary>
/// Behavior / Decorator for View Models to Notify DataServices on Change
/// </summary>
public class DataServiceViewModelListener : IViewModelBehavior
{
    #region Fields
    private DataServiceClient dataClient = null;
    string dataServiceOriginName;
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client">Data Service Client for the Behavior to use</param>
    /// <param name="dataServiceOrigin">Origin used for data key values sent to the DataService</param>
    public DataServiceViewModelListener(DataServiceClient client, string dataServiceOrigin = null)
    {
        if (client == null)
            throw new ArgumentNullException("The DataServiceClient (client) can not be null");

        dataClient = client;
        dataServiceOriginName = dataServiceOrigin;
    }

    void IViewModelBehavior.InitBehavior(ViewModelBase viewModel)
    {
        if (dataServiceOriginName == null)
            dataServiceOriginName = GetType().Name;

        SubscribeToModel();
    }

    void IViewModelBehavior.OnSetValue<T>(T baseValue, T newValue, string memberName) where T : struct
    {
        KeyValue keyValue = GenerateKeyValue(newValue, memberName);
        OnSetDataServiceValue(dataClient, keyValue);
    }

    /// <summary>
    /// The data service client that is going to be used to write the values.
    /// </summary>
    private void SubscribeToModel()
    {
        foreach (var item in GetType().GetProperties())
        {
            var attribute = item.GetCustomAttribute<ViewModelPropertyBaseAttribute>();
            if (attribute != null)
            {
                KeyValue keyValue = GenerateKeyValue(item.GetValue(this), item.Name);
                OnSetDataServiceValue(dataClient, keyValue);
            }
        }
    }

    /// <summary>
    /// Sets the value given a member name into the data service.
    /// </summary>
    /// <param name="newValue">The new value that is going to be assigned in the data service.</param>
    /// <param name="keyString">The key string of the value that is going to be assigned.</param>
    private KeyValue GenerateKeyValue(object newValue, string keyString)
    {
        KeyValue keyValue = new()
        {
            KeyInfo = new()
            {
                Key = keyString,
                Origin = dataServiceOriginName,
                MininumAccessLevel = System.DataAccessLevel.Public,
            }
        };

        keyValue.SetValue(newValue);

        return keyValue;
    }

    /// <summary>
    /// Sets the value given a member name into the data service.
    /// </summary>
    /// <param name="client">Data Service Client.</param>
    /// <param name="keyValue">The Key value that is going to be set on the data service.</param>
    private void OnSetDataServiceValue(DataServiceClient client, KeyValue keyValue)
    {
        KeyValueList keyValueList = new();
        keyValueList.Values.Add(keyValue);
        client.SetKeyValues(keyValueList);
    }
}
