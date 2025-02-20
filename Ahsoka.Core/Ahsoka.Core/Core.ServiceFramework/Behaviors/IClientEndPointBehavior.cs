using System;

namespace Ahsoka.Core;

/// <summary>
/// Base Interface for adding Behaviors to Ahsoka Endpoint Clients
/// </summary>
/// <typeparam name="TTransportIds"></typeparam>
public interface IClientEndPointBehavior<TTransportIds> where TTransportIds : Enum, new()
{
    /// <summary>
    /// Initializes the Behavior once added to a Client
    /// </summary>
    /// <param name="client"></param>
    /// <param name="config"></param>
    void Init(AhsokaClientBase<TTransportIds> client, ServiceConfiguration config);

    /// <summary>
    ///  Notifies the Behavior Object that the object is going to be sent to the Service
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    void OnSendMessage(TTransportIds transportId, object requestObject);

    /// <summary>
    /// Message Received
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    void OnNotificationMessageRecieved(TTransportIds transportId, object requestObject);
}
