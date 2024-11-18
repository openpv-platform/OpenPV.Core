using Ahsoka.Dispatch;
using Ahsoka.Services.Security;
using Ahsoka.System;
using Ahsoka.Utility;
using Azure.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IAhsokaClientEndPoint : IAhsokaEndPoint, IDispatchSource
{
    IAhsokaServiceEndPoint CreateDefaultService();

    byte[] GetClientId();
    byte[] GetNotificationId();
    void SetToken(Token securityToken);
}

public abstract partial class AhsokaClientBase<TTransportIds> : AhsokaEndPoint<TTransportIds>,
    IAhsokaClientEndPoint
    where TTransportIds : Enum, new()
{
    byte[] clientId = Guid.NewGuid().ToByteArray();
    byte[] notificationClientId = Guid.NewGuid().ToByteArray();

    IAhsokaServiceEndPoint autoStartService = null;
    BlockingCollection<KeyValuePair<AhsokaMessageHeader, object>> incomingMessages = new();
    Token securityToken = null;

    #region Service Notification Handler 
    public class AhsokaNotificationArgs : NotificationEventArgs<TTransportIds> { }

    // Declare the event.
    public event EventHandler<AhsokaNotificationArgs> NotificationReceived;

    // Dispatch Event Handler
    event EventHandler<EventArgs> IDispatchSource.HandleDispatchMessage
    {
        add
        {
            NotificationReceived += value.Invoke;
        }

        remove
        {
            NotificationReceived -= value.Invoke;
        }
    }

    #endregion

    #region Fields
    List<IClientEndPointBehavior<TTransportIds>> behaviors = new();
    int messageId = 0;
    #endregion

    #region Methods
    protected AhsokaClientBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    /// <summary>
    /// Unique ID for this Clients Message Channel
    /// </summary>
    /// <returns></returns>
    public byte[] GetClientId() { return clientId; }

    /// <summary>
    /// Unique ID for this Clients Notification Channel
    /// </summary>
    /// <returns></returns>
    public byte[] GetNotificationId() { return notificationClientId; }

    protected override void OnEndPointConnecting()
    {
        base.OnEndPointConnecting();

        AutoStartServiceIfNeeded();
    }

    protected override void OnEndPointConnected()
    {
        base.OnEndPointConnected();

        // Register Client for Notifications
        UpdateClientRegistration(CoreTransportIds.RegisterClient);
    }
   

    private void UpdateClientRegistration(CoreTransportIds transportId)
    {
        AhsokaMessageHeader notificationRequest = new()
        {
            TransportId = transportId.EnumToInt(),
            ClientMessageId = messageId++,
            ServiceToken = securityToken?.ClientToken
        };

        SendMessageInternal(notificationRequest, new ClientRegistration()
        {
            ClientId = ((IAhsokaClientEndPoint)this).GetNotificationId(),
        });
    }

    protected override void OnEndPointDisconnected()
    {
        // Deregister Client for Notifications
        UpdateClientRegistration(CoreTransportIds.DeregisterClient);

        if (autoStartService != null)
            autoStartService.Stop();

        base.OnEndPointDisconnected();
    }

    protected override IAhsokaSocket CreateSocket(ServiceConfiguration config)
    {
        if (config.SocketType == TransportSocketTypes.TcpSocket)
            return new TCPClientSocket();
        else
            throw new Exception("Socket Type is Not Supported");
    }

    public void SetToken(Token securityToken)
    {
        this.securityToken = securityToken;
    }

    public void AddBehavior(IClientEndPointBehavior<TTransportIds> behavior)
    {
        behavior.Init(this, ServiceConfig);
        behaviors.Add(behavior);
    }

    protected override void OnReceiveMessage(AhsokaMessageHeader messageHeader, object message, ServiceMessageInfo info)
    {
        if (info == null)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"No transport Info was found for Message with TransportID = {messageHeader.TransportId}. Message was passed through as a Notification");
            OnHandleReceive(messageHeader, message);
        }
        else if (info.IsNotification && messageHeader.TransportId >= 0)
        {
            // Error Notifications are < 0 and Reply to Original Message Thread
            OnHandleReceive(messageHeader, message);
        }
        else
        {
            // Replies and Error Responses
            incomingMessages.TryAdd(new(messageHeader, message));
        }
    }

    public void SendMessageWithResponse(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse<EmptyNotification>(transportId, requestObject, timeoutInMs);
    }

    public virtual T SendMessageWithResponse<T>(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue) where T : class
    {
        if (!IsConnected)
            Start();

        foreach (IClientEndPointBehavior<TTransportIds> behavior in behaviors)
            behavior.OnSendMessage(transportId, requestObject);

        AhsokaMessageHeader header = new()
        {
            TransportId = transportId.EnumToInt(),
            ClientMessageId = messageId++,
            ServiceToken = securityToken?.ClientToken
        };

        SendMessageInternal(header, requestObject);
        return WaitForResponse<T>(header, timeoutInMs);
    }

    protected virtual void AutoStartServiceIfNeeded()
    {
        // Handle AutoStartServices
        if (autoStartService == null &&  ServiceConfig.Behaviors.HasFlag(EndPointBehaviors.AutoStart))
        {
            autoStartService = ((IAhsokaClientEndPoint)this).CreateDefaultService();
            autoStartService.Start();
        }
    }

    protected T WaitForResponse<T>(AhsokaMessageHeader requestHeader, int timeoutInMs = int.MaxValue) where T : class
    {
        AhsokaMessageHeader responseHeader = null;
        object returnObject = null;

        var cancelTime = DateTime.Now.Add(TimeSpan.FromMilliseconds(timeoutInMs));

        // We found our Message or Timed out Waiting for Messages
        while (responseHeader?.TransportId != requestHeader.TransportId 
                && responseHeader?.ClientMessageId != requestHeader.ClientMessageId 
                && cancelTime > DateTime.Now)
        {
            if (incomingMessages.TryTake(out var message, timeoutInMs))
            {
                responseHeader = message.Key;
                returnObject = message.Value;

                if (responseHeader.TransportId == CoreTransportIds.InvalidToken.EnumToInt())
                    throw new SecurityException("The Service returned an InvalidToken Message. This usually indicates that this security enabled endpoint was not unlocked with a valid token");

                if (responseHeader.TransportId == CoreTransportIds.ServiceErrorThrown.EnumToInt() &&
                    returnObject is ServiceErrorThrownResponse errorMessage)
                {
                    throw new SecurityException($"The Service {errorMessage.ServiceName} returned an error. {errorMessage.ErrorInformation}");
                }
            }
        }

        return returnObject as T;
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        var args = new AhsokaNotificationArgs()
        {
            TransportId = messageHeader.TransportId.IntToEnum<TTransportIds>(),
            NotificationObject = message
        };

        foreach (IClientEndPointBehavior<TTransportIds> behavior in behaviors)
            behavior.OnNotificationMessageRecieved(args.TransportId, args.NotificationObject);

        NotificationReceived?.Invoke(this, args);
    }

    protected override Type GetReceiptType(ServiceMessageInfo value)
    {
        // We recieve replies
        return value.ReplyType;
    }

    IAhsokaServiceEndPoint IAhsokaClientEndPoint.CreateDefaultService()
    {
        return OnCreateDefaultService();
    }

    protected abstract IAhsokaServiceEndPoint OnCreateDefaultService();

    #endregion
}

public class NotificationEventArgs<T> : EventArgs
{
    public T TransportId { get; set; }
    public object NotificationObject { get; set; }
}
#pragma warning restore CS1591