using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Diagnostics;

#pragma warning disable CS1591

namespace Ahsoka.ServiceFramework;

#region Client Socket 
public sealed class TCPClientSocket : AhsokaSocket
{
    public byte[] clientId = Guid.NewGuid().ToByteArray();
    NetMQPoller SocketPoller { get; set; }
    DealerSocket Socket { get; set; }
    SubscriberSocket NotificationSocket { get; set; }

    internal override void Connect()
    {
        try
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Client {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataURI}");

            SocketPoller = new NetMQPoller();
            Socket = new DealerSocket();
            Socket.Options.Linger = TimeSpan.FromSeconds(1);
            Socket.Options.Identity = clientId;

            Socket.Connect(EndPoint.ServiceConfig.DataURI);

            if (!String.IsNullOrEmpty(EndPoint.ServiceConfig.NotificationURI))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Starting Notification {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.NotificationURI}");

                NotificationSocket = new SubscriberSocket();
                NotificationSocket.Connect(EndPoint.ServiceConfig.NotificationURI);
                NotificationSocket.SubscribeToAnyTopic();

                // Handle Poller for Notifications
                SocketPoller.Add(NotificationSocket);
                NotificationSocket.ReceiveReady += (o, e) =>
                {
                    ((IAhsokaClientEndpoint)EndPoint).NotificationDataReady();
                };
                SocketPoller.RunAsync($"{EndPoint.ServiceConfig.ServiceName}_NotificationListener");
            }

            IsConnected = true;
        }
        catch { IsConnected = false; }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Client {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataURI} with Result {IsConnected}");
    }

    internal override void Disconnect()
    {
        if (IsConnected)
        {
            try
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {EndPoint.ServiceConfig.ServiceName}");

                Socket.Disconnect(EndPoint.ServiceConfig.DataURI);

                // If we have a Notificaiotn Socket, Destroy it Here.
                if (NotificationSocket != null)
                {
                    try { Socket.Disconnect(EndPoint.ServiceConfig.NotificationURI); } catch { }

                    SocketPoller.Stop();
                    SocketPoller.RemoveAndDispose(Socket);
                }

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {EndPoint.ServiceConfig.ServiceName}");
            }
            catch { }
            finally { IsConnected = false; }
        }
    }

    internal override void SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Client {(sendAsNotification ? "Notification" : "Message")} from {EndPoint.ServiceConfig.ServiceName} ");

        NetMQSocket socket = sendAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();
        message.Append(header);
        message.Append(data);

        lock (socket)
            socket.SendMultipartMessage(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client {(sendAsNotification ? "Notification" : "Message")} Completed from {EndPoint.ServiceConfig.ServiceName} ");

    }

    internal override bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs = int.MaxValue)
    {
        if (timeOutInMs > maxTimeoutValue)
            timeOutInMs = maxTimeoutValue;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client {(receiveAsNotification ? "Notification" : "Message")} on {EndPoint.ServiceConfig.ServiceName} with Timeout {timeOutInMs}");

        header = data = clientId = null;

        // Select Socket to Recieve
        NetMQSocket socket = receiveAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();
        if (socket != null && socket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(timeOutInMs), ref message, 2))
        {
            header = message.Pop().Buffer;
            data = message.Pop().Buffer;
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Received Client {(receiveAsNotification ? "Notification" : "Message")} on {EndPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }
}
#endregion

#region Service Socket 
public sealed class TCPServiceSocket : AhsokaSocket
{
    NetMQPoller SocketPoller { get; set; }
    RouterSocket Socket { get; set; }
    NetMQMonitor Monitor { get; set; }
    PublisherSocket NotificationSocket { get; set; }

    internal override void Connect()
    {
        try
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Service {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataListenURI}");
            SocketPoller = new NetMQPoller();
            Socket = new RouterSocket();
            Socket.Options.Linger = TimeSpan.FromSeconds(1);
            Socket.Bind(EndPoint.ServiceConfig.DataListenURI);
            SocketPoller.Add(Socket);

            Monitor = new NetMQMonitor(Socket, $"inproc://{Guid.NewGuid().ToString()}.inproc", SocketEvents.All);
            Monitor.AttachToPoller(SocketPoller);

            if (!String.IsNullOrEmpty(EndPoint.ServiceConfig.NotificationURI))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Starting Notification @ {EndPoint.ServiceConfig.NotificationListenURI}");
                NotificationSocket = new PublisherSocket();
                NotificationSocket.Bind(EndPoint.ServiceConfig.NotificationURI);
            }

            IsConnected = true;

            Socket.ReceiveReady += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)EndPoint).ServiceDataReady();
            };

            Monitor.Accepted += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)EndPoint).ClientConnected();
            };
          
            SocketPoller.RunAsync($"{EndPoint.ServiceConfig.ServiceName}_Handler");

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Service {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataListenURI} with Result {IsConnected}");
        }
        catch { IsConnected = false; }
    }

    internal override void Disconnect()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {EndPoint.ServiceConfig.ServiceName}");

        if (IsConnected)
        {
            Monitor.DetachFromPoller();
            Socket.Unbind(EndPoint.ServiceConfig.DataListenURI);
            NotificationSocket?.Unbind(EndPoint.ServiceConfig.NotificationURI);
        }

        IsConnected = false;

        SocketPoller.Stop();
        SocketPoller.RemoveAndDispose(Socket);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {EndPoint.ServiceConfig.ServiceName}");

    }

    internal override void SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service {(sendAsNotification ? "Notification" : "Message")} on {EndPoint.ServiceConfig.ServiceName} ");

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

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service {(sendAsNotification ? "Notification" : "Message")} on {EndPoint.ServiceConfig.ServiceName} ");
    }

    internal override bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs = int.MaxValue)
    {
        if (timeOutInMs > maxTimeoutValue)
            timeOutInMs = maxTimeoutValue;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Service {(receiveAsNotification ? "Notification" : "Message")} on {EndPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        NetMQSocket socket = receiveAsNotification ? NotificationSocket : Socket;

        NetMQMessage message = new();
        if (socket.TryReceiveMultipartMessage(ref message, 3))
        {
            clientId = message.Pop().Buffer;
            header = message.Pop().Buffer;
            data = message.Pop().Buffer;
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Recieved Service {(receiveAsNotification ? "Notification" : "Message")} on {EndPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }
}
#endregion
#pragma warning restore CS1591