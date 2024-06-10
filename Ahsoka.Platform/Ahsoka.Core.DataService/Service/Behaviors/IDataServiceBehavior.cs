using Ahsoka.Services.Data;
using Ahsoka.System;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Behavior Interface for DataService
/// </summary>
public interface IDataServiceBehavior : IClientEndPointBehavior<DataMessageTypes.Ids>
{
    /// <summary>
    ///  Called when the Data Service Behavior is added to a Data Service Client.
    /// </summary>
    /// <param name="dataServiceClient"></param>
    void Init(AhsokaClientBase<DataMessageTypes.Ids> dataServiceClient);

    /// <summary>
    /// Handle Notification from Data SErvice
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    void OnDataServiceNotificationReceived(DataMessageTypes.Ids transportId, object requestObject);

    #region Adapter for IClientEndPointBehavior
    void IClientEndPointBehavior<DataMessageTypes.Ids>.OnNotificationMessageRecieved(DataMessageTypes.Ids transportId, object requestObject)
    {
        OnDataServiceNotificationReceived(transportId, requestObject);
    }


    void IClientEndPointBehavior<DataMessageTypes.Ids>.Init(AhsokaClientBase<DataMessageTypes.Ids> client, Ahsoka.System.ServiceConfiguration config)
    {
        Init(client);
    }

    void IClientEndPointBehavior<DataMessageTypes.Ids>.OnSendMessage(DataMessageTypes.Ids transportId, object requestObject)
    {
        // Not Needed by this interface
    }
    #endregion
}
