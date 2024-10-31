using NetMQ;
using NetMQ.Sockets;
using System;
using System.Net.Sockets;

namespace Ahsoka.ServiceFramework;

internal sealed class TCPClientSocket : IAhsokaSocket
{
    IAhsokaClientEndPoint endPoint;

    NetMQPoller socketPoller = new();
    DealerSocket socket;
    NetMQQueue<NetMQMessage> sendQueue = new();
    DealerSocket notificationSocket;

    public bool IsConnected { get; private set; }

    public TCPClientSocket() { }

    void IAhsokaSocket.Connect(IAhsokaEndPoint endPoint)
    {
        try
        {

            this.endPoint = endPoint as IAhsokaClientEndPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI}");

            // Message Socket
            socket = new DealerSocket();
            socket.Options.Identity = this.endPoint.GetClientId();
            socket.Options.Linger = TimeSpan.FromSeconds(1);
            socket.Connect(this.endPoint.ServiceConfig.DataURI);
            socketPoller.Add(socket);
            socketPoller.Add(sendQueue);

            // Listen to Messages
            socket.ReceiveReady += (o, e) =>
            {
                this.endPoint.ReceiveMessage(false);
            };

            sendQueue.ReceiveReady += (o, e) =>
            {
                while (sendQueue.Count > 0)
                    socket.TrySendMultipartMessage(sendQueue.Dequeue());
            };

            // Notification Socket
            notificationSocket = new DealerSocket();
            notificationSocket.Options.Identity = this.endPoint.GetNotificationId();
            notificationSocket.Options.Linger = TimeSpan.FromSeconds(1);
            notificationSocket.Connect(this.endPoint.ServiceConfig.DataURI);
            socketPoller.Add(notificationSocket);
            
            // Listen to Notifications
            notificationSocket.ReceiveReady += (o, e) =>
            {
                this.endPoint.ReceiveMessage(true);
            };

            socketPoller.RunAsync($"{this.endPoint.ServiceConfig.ServiceName}_ClientPoller");

            IsConnected = true;


            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI} with Result {IsConnected}");
        }
        catch { IsConnected = false; }

    }

    void IAhsokaSocket.Disconnect()
    {
        if (IsConnected)
        {
            try
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {endPoint.ServiceConfig.ServiceName}");

                socket.Disconnect(endPoint.ServiceConfig.DataURI);
                notificationSocket.Disconnect(endPoint.ServiceConfig.DataURI);
                socketPoller.StopAsync();
                socketPoller.RemoveAndDispose(socket);
                socketPoller.RemoveAndDispose(sendQueue);
                socketPoller.RemoveAndDispose(notificationSocket);

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");
            }
            catch { }
            finally { IsConnected = false; }
        }
    }

    void IAhsokaSocket.SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Client {(sendAsNotification ? "Notification" : "Message")} from {endPoint.ServiceConfig.ServiceName} ");

        NetMQMessage message = new();
        message.Append(header);
        message.Append(data);

        sendQueue.Enqueue(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client {(sendAsNotification ? "Notification" : "Message")} Completed from {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool isNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client Message on {endPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        DealerSocket recieveSocket = isNotification ? notificationSocket : socket; 

        NetMQMessage message = new();
        try
        {
            if (recieveSocket != null && recieveSocket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(1000), ref message, 2))
            {
                header = message.Pop().Buffer;
                data = message.Pop().Buffer;
            }
        }
        catch (SocketException) { } // Catching Errors During Shutdown / Disconnect

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Received Client Message on {endPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }

}
