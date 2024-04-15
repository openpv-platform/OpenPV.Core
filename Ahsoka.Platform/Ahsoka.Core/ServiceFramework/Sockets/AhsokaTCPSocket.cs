using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Diagnostics;

namespace Ahsoka.ServiceFramework;

#region Client Socket 
internal sealed class TCPClientSocket : IAhsokaSocket
{
    IAhsokaEndPoint endPoint;

    NetMQPoller SocketPoller { get; set; }
    DealerSocket Socket { get; set; }
    SubscriberSocket NotificationSocket { get; set; }

    public bool IsConnected { get; private set; }

    public TCPClientSocket() { }

    void IAhsokaSocket.Connect(IAhsokaEndPoint endPoint)
    {
        try
        {
            this.endPoint = endPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI}");

            SocketPoller = new NetMQPoller();
            Socket = new DealerSocket();
            Socket.Options.Linger = TimeSpan.FromSeconds(1);
            Socket.Options.Identity = endPoint.GetEndPointId();

            Socket.Connect(this.endPoint.ServiceConfig.DataURI);

            if (!string.IsNullOrEmpty(this.endPoint.ServiceConfig.NotificationURI))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Starting Notification {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.NotificationURI}");

                NotificationSocket = new SubscriberSocket();
                NotificationSocket.Connect(this.endPoint.ServiceConfig.NotificationURI);
                NotificationSocket.SubscribeToAnyTopic();

                // Handle Poller for Notifications
                SocketPoller.Add(NotificationSocket);
                NotificationSocket.ReceiveReady += (o, e) =>
                {
                    ((IAhsokaClientEndpoint)this.endPoint).NotificationDataReady();
                };
                SocketPoller.RunAsync($"{this.endPoint.ServiceConfig.ServiceName}_NotificationListener");
            }

            IsConnected = true;
        }
        catch { IsConnected = false; }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI} with Result {IsConnected}");
    }

    void IAhsokaSocket.Disconnect()
    {
        if (IsConnected)
        {
            try
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {endPoint.ServiceConfig.ServiceName}");

                Socket.Disconnect(endPoint.ServiceConfig.DataURI);

                // If we have a Notificaiotn Socket, Destroy it Here.
                if (NotificationSocket != null)
                {
                    try { Socket.Disconnect(endPoint.ServiceConfig.NotificationURI); } catch { }

                    SocketPoller.Stop();
                    SocketPoller.RemoveAndDispose(Socket);
                }

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");
            }
            catch { }
            finally { IsConnected = false; }
        }
    }

    void IAhsokaSocket.SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Client {(sendAsNotification ? "Notification" : "Message")} from {endPoint.ServiceConfig.ServiceName} ");

        NetMQSocket socket = sendAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();
        message.Append(header);
        message.Append(data);

        lock (socket)
            socket.SendMultipartMessage(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client {(sendAsNotification ? "Notification" : "Message")} Completed from {endPoint.ServiceConfig.ServiceName} ");

    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs)
    {
        if (timeOutInMs > IAhsokaSocket.MaxTimeoutValue)
            timeOutInMs = IAhsokaSocket.MaxTimeoutValue;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} with Timeout {timeOutInMs}");

        header = data = clientId = null;

        // Select Socket to Recieve
        NetMQSocket socket = receiveAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();
        if (socket != null && socket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(timeOutInMs), ref message, 2))
        {
            header = message.Pop().Buffer;
            data = message.Pop().Buffer;
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Received Client {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }

}
#endregion

#region Service Socket 
internal sealed class TCPServiceSocket : IAhsokaSocket
{
    IAhsokaEndPoint endPoint;
    byte[] socketId = Guid.NewGuid().ToByteArray();

    NetMQPoller SocketPoller { get; set; }
    RouterSocket Socket { get; set; }
    NetMQMonitor Monitor { get; set; }
    PublisherSocket NotificationSocket { get; set; }

    public bool IsConnected { get; private set; }

    public TCPServiceSocket() { }

    void IAhsokaSocket.Connect(IAhsokaEndPoint endPoint)
    {
        try
        {
            this.endPoint = endPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI}");
            SocketPoller = new NetMQPoller();
            Socket = new RouterSocket();
            Socket.Options.Linger = TimeSpan.FromSeconds(1);
            Socket.Bind(endPoint.ServiceConfig.DataListenURI);
            SocketPoller.Add(Socket);

            Monitor = new NetMQMonitor(Socket, $"inproc://{Guid.NewGuid().ToString()}.inproc", SocketEvents.All);
            Monitor.AttachToPoller(SocketPoller);

            if (!String.IsNullOrEmpty(endPoint.ServiceConfig.NotificationURI))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Starting Notification @ {endPoint.ServiceConfig.NotificationListenURI}");
                NotificationSocket = new PublisherSocket();
                NotificationSocket.Bind(endPoint.ServiceConfig.NotificationURI);
            }

            IsConnected = true;

            Socket.ReceiveReady += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)endPoint).ServiceDataReady();
            };

            Monitor.Accepted += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)endPoint).ClientConnected();
            };
          
            SocketPoller.RunAsync($"{endPoint.ServiceConfig.ServiceName}_Handler");

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI} with Result {IsConnected}");
        }
        catch { IsConnected = false; }
    }

    void IAhsokaSocket.Disconnect()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {endPoint.ServiceConfig.ServiceName}");

        if (IsConnected)
        {
            Monitor.DetachFromPoller();
            Socket.Unbind(endPoint.ServiceConfig.DataListenURI);
            NotificationSocket?.Unbind(endPoint.ServiceConfig.NotificationURI);
        }

        IsConnected = false;

        SocketPoller.Stop();
        SocketPoller.RemoveAndDispose(Socket);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");

    }

    void IAhsokaSocket.SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        if (clientId == null && !sendAsNotification)
            throw new ApplicationException("Services must include client id when sending messages");

        NetMQSocket socket = sendAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();

        // Notifications are Broadcast, not sent to specific client
        if (!sendAsNotification)
            message.Append(clientId);

        message.Append(header);
        message.Append(data);

        lock (socket) // Protect Socket
            socket.SendMultipartMessage(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs)
    {
        if (timeOutInMs > IAhsokaSocket.MaxTimeoutValue)
            timeOutInMs = IAhsokaSocket.MaxTimeoutValue;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Service {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        NetMQSocket socket = receiveAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();
        if (socket.TryReceiveMultipartMessage(ref message, 3))
        {
            clientId = message.Pop().Buffer;
            header = message.Pop().Buffer;
            data = message.Pop().Buffer;
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Recieved Service {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }
}
#endregion
