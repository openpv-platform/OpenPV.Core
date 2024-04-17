using Ahsoka.Dispatch;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IAhsokaClientEndpoint : IAhsokaEndPoint, IDispatchSource
{
    IAhsokaServiceEndpoint CreateDefaultService();

    void NotificationDataReady();
}

public abstract class AhsokaClientBase<TTransportIds> : AhsokaEndPoint<TTransportIds>,
    IAhsokaClientEndpoint
    where TTransportIds : Enum, new()
{
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
    List<IClientEndPointBehavior<TTransportIds>> behaviors = new ();
    int messageId = 0;
    #endregion

    #region Methods
    protected AhsokaClientBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {
        if (config.Behaviors.HasFlag(EndPointBehaviors.AutoStart))
            AddBehavior(new AutoStartClientEndPointBehavior<TTransportIds>());
    }

    protected override IAhsokaSocket CreateSocket(ServiceConfiguration config)
    {
        if (config.SocketType == TransportSocketTypes.TcpSocket)
            return new TCPClientSocket();
        else
            throw new Exception("Socket Type is Not Supported");
    }

    public void AddBehavior(IClientEndPointBehavior<TTransportIds> behavior)
    {
        behavior.Init(this, ServiceConfig);
        behaviors.Add(behavior);
    }

    void IAhsokaClientEndpoint.NotificationDataReady()
    {
        // Recieve Out of Band Message and Process on Handler
        if (ReceiveMessage(out AhsokaMessageHeader responseHeader, out object returnObject, true))
            OnHandleReceive(responseHeader, returnObject);
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
            ClientMessageId = messageId++
        };

        lock (Socket)
        {
            SendMessageInternal(header, requestObject);

            return WaitForResponse<T>(header, timeoutInMs);
        }
    }

    protected T WaitForResponse<T>(AhsokaMessageHeader requestHeader, int timeoutInMs = int.MaxValue) where T : class
    {
        AhsokaMessageHeader responseHeader = null;
        object returnObject = null;

        var cancelTime = DateTime.Now.Add(TimeSpan.FromMilliseconds(timeoutInMs));

        // We found our Message or Timed out Waiting for Messages
        while (responseHeader?.TransportId != requestHeader.TransportId && cancelTime > DateTime.Now)
        {
            if (ReceiveMessage(out responseHeader, out returnObject, false, timeoutInMs))
            {
                // If Async Message...send to Out of Band Handler
                if (responseHeader.TransportId != requestHeader.TransportId)
                    OnHandleReceive(responseHeader, returnObject);
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