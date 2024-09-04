using Ahsoka.Services.Security;
using Ahsoka.System;
using Ahsoka.Utility;
using Azure;
using System;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591


public interface IAhsokaServiceEndPoint : IAhsokaEndPoint
{
    int MessageReceiveCount { get; set; }
    int MessageSendCount { get; set; }

    void ClientConnected();
}


public abstract class AhsokaServiceBase<TTransportIds> : AhsokaEndPoint<TTransportIds>, IAhsokaServiceEndPoint
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
            return new TCPServiceSocket();
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

    protected override void OnReceiveMessage(AhsokaMessageHeader messageHeader, object message, ServiceMessageInfo info)
    {
        // Handle Global Notification Registration
        if (messageHeader.TransportId == CoreTransportIds.RegisterClient.EnumToInt())
        {
            ((IAhsokaServiceSocket)Socket)?.RegisterClient(message as ClientRegistration);
            ((IAhsokaServiceEndPoint)this).ClientConnected();
            return;
        }
        else if (messageHeader.TransportId == CoreTransportIds.DeregisterClient.EnumToInt())
        {
            ((IAhsokaServiceSocket)Socket)?.DeregisterClient(message as ClientRegistration);
            return;
        }

        // Validate Security
        if (this.ServiceConfig.Behaviors.HasFlag(EndPointBehaviors.SecurityEnabled) && info.RequiresSecurity)
        {
            if (messageHeader.ServiceToken == null || !TokenSource.Default.IsTokenValid(messageHeader.ServiceToken, messageHeader.ClientId))
            {
                // Send Invalid Transport Value
                messageHeader.TransportId = CoreTransportIds.InvalidToken.EnumToInt();
                SendMessageInternal(messageHeader, new InvalidResponseNotification());
                return;
            }
        }
        try
        {
            OnHandleReceive(messageHeader, message);
        }
        catch (Exception ex)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High,$"An error occured while handling a service message in {this.ServiceConfig.ServiceName} \r\n {ex.ToString()}");
            messageHeader.TransportId = CoreTransportIds.ServiceErrorThrown.EnumToInt();
            SendMessageInternal(messageHeader, new ServiceErrorThrownResponse()
            {
                ServiceName = this.ServiceConfig.ServiceName,
                ErrorInformation = ex.Message,
            });
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

    void IAhsokaServiceEndPoint.ClientConnected()
    {
        OnClientConnected();
    }

    public int UpdateCacheValue(string key, object value)
    {
        if (IServiceCache.Default == null)
            return -1;

        return IServiceCache.Default.UpdateCacheValue(this.ServiceConfig.ServiceName, key, value, publisherId);
    }

    public int UpdateEvent(EventData serviceEvent)
    {
        if (IServiceEvent.Default == null)
            return -1;

        return IServiceEvent.Default.SendEvent(serviceEvent, publisherId);
    }

    #endregion
}

#pragma warning restore CS1591