using Ahsoka.Dispatch;
using Ahsoka.Services.Security;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IAhsokaClientEndpoint : IAhsokaEndPoint, IDispatchSource
{
    IAhsokaServiceEndpoint CreateDefaultService();

    byte[] GetClientId();
    byte[] GetNotificationId();
    void SetToken(Token securityToken);

}

public abstract class AhsokaClientBase<TTransportIds> : AhsokaEndPoint<TTransportIds>,
    IAhsokaClientEndpoint
    where TTransportIds : Enum, new()
{
    byte[] clientId = Guid.NewGuid().ToByteArray();
    byte[] notificationClientId = Guid.NewGuid().ToByteArray();

    Task notificationTask = null;
    CancellationTokenSource source;
    BlockingCollection<KeyValuePair<AhsokaMessageHeader, object>> incomingMessages = new();
    BlockingCollection<KeyValuePair<AhsokaMessageHeader, object>> notificationMessages = new();
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
        if (config.Behaviors.HasFlag(EndPointBehaviors.AutoStart))
            AddBehavior(new AutoStartClientEndPointBehavior<TTransportIds>());
    }

    byte[] IAhsokaClientEndpoint.GetClientId() { return clientId; }
    byte[] IAhsokaClientEndpoint.GetNotificationId() { return notificationClientId; }

    protected override void OnEndPointConnected()
    {
        base.OnEndPointConnected();

        // Register Client for Notifications
        UpdateClientRegistration(CoreTransportIds.RegisterClient);

        // Start Notification Listener if this service has callbacks
        if (this.ServiceMessageInfo.Info.Any(x => x.Value.IsNotification))
        {
            incomingMessages = new();
            source = new CancellationTokenSource();
            try
            {
                notificationTask = Task.Factory.StartNew(() =>
                {
                    // Set Thread Name
                    var originalName = Thread.CurrentThread.Name;
                    Thread.CurrentThread.Name = $"{this.ServiceConfig.ServiceName}_NotificationHandler";

                    while (!source.IsCancellationRequested)
                        if (notificationMessages.TryTake(out var message, int.MaxValue, source.Token))
                            OnHandleReceive(message.Key, message.Value);

                    // Restore Name since this comes from the Thread Pool
                    Thread.CurrentThread.Name = originalName;

                }, source.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (OperationCanceledException) { }
        }
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
            ClientId = ((IAhsokaClientEndpoint)this).GetNotificationId(),
        });
    }

    protected override void OnEndPointDisconnected()
    {
        // Deregister Client for Notifications
        UpdateClientRegistration(CoreTransportIds.DeregisterClient);

        base.OnEndPointDisconnected();

        source.Cancel();
        notificationTask?.Wait();
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
            notificationMessages.TryAdd(new(messageHeader, message));
        }
        else if (info.IsNotification && messageHeader.TransportId >= 0) //Error Notifications are < 0
            notificationMessages.TryAdd(new(messageHeader, message));
        else
            incomingMessages.TryAdd(new(messageHeader, message));
    }

    public void SendNotificationWithResponse(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(transportId, requestObject, timeoutInMs);
    }

    public virtual T SendNotificationWithResponse<T>(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue) where T : class
    {
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

    protected T WaitForResponse<T>(AhsokaMessageHeader requestHeader, int timeoutInMs = int.MaxValue) where T : class
    {
        AhsokaMessageHeader responseHeader = null;
        object returnObject = null;

        var cancelTime = DateTime.Now.Add(TimeSpan.FromMilliseconds(timeoutInMs));

        // We found our Message or Timed out Waiting for Messages
        while (responseHeader?.TransportId != requestHeader.TransportId && cancelTime > DateTime.Now)
        {
            if (incomingMessages.TryTake(out var message, timeoutInMs))
            {
                responseHeader = message.Key;
                returnObject = message.Value;

                if (responseHeader.TransportId == CoreTransportIds.InvalidToken.EnumToInt())
                    throw new SecurityException("The Service returned an InvalidToken Message. This usually indicates that this security enabled endpoint was not unlocked iwht a valid token");
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

    IAhsokaServiceEndpoint IAhsokaClientEndpoint.CreateDefaultService()
    {
        return OnCreateDefaultService();
    }

    protected abstract IAhsokaServiceEndpoint OnCreateDefaultService();

    #endregion
}

public class NotificationEventArgs<T> : EventArgs
{
    public T TransportId { get; set; }
    public object NotificationObject { get; set; }
}
#pragma warning restore CS1591