using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591


public interface IAhsokaServiceEndpoint : IAhsokaEndPoint
{
    int MessageReceiveCount { get; set; }
    int MessageSendCount { get; set; }

    void ClientConnected();

    void ServiceDataReady();
}


public abstract class AhsokaServiceBase<TTransportIds> : AhsokaEndPoint<TTransportIds>, IAhsokaServiceEndpoint
    where TTransportIds : Enum, new()
{
    #region Fields
    internal static bool IsServiceStarted { get; private set; }
    private string publisherId => ServiceConfig.ServiceName;
    #endregion

    #region Methods
    protected AhsokaServiceBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    protected override IAhsokaSocket CreateSocket(ServiceConfiguration config)
    {
        if (config.SocketType == TransportSocketTypes.TcpSocket)
            return new TCPServiceSocket() ;
        else
            throw new Exception("Socket Type is Not Supported");
    }

    protected virtual AhsokaMessageHeader CreateReplyMessageHeader(AhsokaMessageHeader header, TTransportIds transportId)
    {
        return new AhsokaMessageHeader()
        {
            TransportId = transportId.EnumToInt(),
            ClientMessageId = header.ClientMessageId,
            ClientId = header.ClientId,
        };
    }

    protected override Type GetReceiptType(ServiceMessageInfo value)
    {
        // We recieve Requests (Service)
        return value.RequestType;
    }

    void IAhsokaServiceEndpoint.ServiceDataReady()
    {
        // Recieve In Band Data
        if (ReceiveMessage(out AhsokaMessageHeader responseHeader, out object returnObject, false))
        {
            OnHandleReceive(responseHeader, returnObject);
        }
    }

    protected override void OnEndPointConnected()
    {
        base.OnEndPointConnected();
        IsServiceStarted = true;
    }

    protected override void OnEndPointDisconnected()
    {
        base.OnEndPointDisconnected();
        IsServiceStarted = false;
    }

    void IAhsokaServiceEndpoint.ClientConnected()
    {
        OnClientConnected();
    }

    public int UpdateCacheValue(string key, object value)
    {
        if (IServiceCache.Default == null)
            return -1;

        return IServiceCache.Default.UpdateCacheValue(this.ServiceConfig.ServiceName, key, value, publisherId);
    }

    #endregion
}

#pragma warning restore CS1591