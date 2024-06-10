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

public abstract class AhsokaClientBase<TTransportIds, TAhsokaSocket> : AhsokaEndPoint<TTransportIds, TAhsokaSocket>,
    IAhsokaClientEndpoint
    where TTransportIds : Enum
    where TAhsokaSocket : AhsokaSocket, new()
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

    int messageId = 0;
    #endregion

    #region Methods
    protected AhsokaClientBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {
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
        NotificationReceived?.Invoke(this, new AhsokaNotificationArgs()
        {
            TransportId = messageHeader.TransportId.IntToEnum<TTransportIds>(),
            NotificationObject = message
        });
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