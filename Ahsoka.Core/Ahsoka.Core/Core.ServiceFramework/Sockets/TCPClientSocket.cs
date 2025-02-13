using Ahsoka.Services.System;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.IO;
using System.Net.Sockets;

namespace Ahsoka.Core;
/// <summary>
/// Client Socket Handling TCP Connections
/// </summary>
public sealed class TCPClientSocket : IAhsokaClientSocket
{
    IAhsokaClientEndPoint endPoint;
    bool isConnected = false;

    // Main Socket Management
    NetMQPoller socketPoller = new();
    DealerSocket socket;
    NetMQQueue<AhsokaClientMessage> sendQueue = new();

    // Notification Handler
    NetMQPoller notificationPoller = new();
    DealerSocket notificationSocket;

    /// <summary>
    /// TCPClientSocket
    /// </summary>
    public TCPClientSocket() { }

    void IAhsokaClientSocket.Connect(IAhsokaClientEndPoint endPoint)
    {
        try
        {
            this.endPoint = endPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI}");

            // Message Socket
            socket = new DealerSocket();
            socket.Options.Identity = this.endPoint.GetClientId();
            socket.Options.Linger = TimeSpan.FromSeconds(1);
            socket.Connect(this.endPoint.ServiceConfig.DataURI);
            socketPoller.Add(socket);
            socketPoller.Add(sendQueue);
         
            socket.ReceiveReady += (o, e) =>
            {
                AhsokaClientMessage response = RecieveMessage(socket);
                endPoint.HandleResponse(response);
            };

            sendQueue.ReceiveReady += (o, e) =>
            {
                while (sendQueue.Count > 0 && isConnected)
                {
                    var request = sendQueue.Dequeue();
                    if (request != null)
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Client Message from {endPoint.ServiceConfig.ServiceName} ");

                        // Prepare Response Message 
                        using MemoryStream headerStream = new();
                        ProtoBuf.Serializer.Serialize(headerStream, request.Header);

                        NetMQMessage message = new();
                        message.Append(headerStream.ToArray());
                        message.Append(request.MessageData);

                        bool result = false;
                        try
                        {
                            if (socket != null)
                                result = socket.TrySendMultipartMessage(message);
                        }
                        catch (Exception) { }

                        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client Message Completed from {endPoint.ServiceConfig.ServiceName} ");
                    }
                }
            };

            // Notification Socket
            notificationSocket = new DealerSocket();
            notificationSocket.Options.Identity = this.endPoint.GetNotificationId();
            notificationSocket.Options.Linger = TimeSpan.FromSeconds(1);
            notificationSocket.Connect(this.endPoint.ServiceConfig.DataURI);
            notificationPoller.Add(notificationSocket);

            // Listen to Notifications
            notificationSocket.ReceiveReady += (o, e) =>
            {
                // Receive Message Data
                AhsokaClientMessage message = RecieveMessage(notificationSocket);

                // Send to Endpoint to Route / Handle
                this.endPoint.HandleNotification(message);
            };

            socketPoller.RunAsync($"{this.endPoint.ServiceConfig.ServiceName}_Client Socket Thread");
            notificationPoller.RunAsync($"{this.endPoint.ServiceConfig.ServiceName}_Client_Notification Thread");

            isConnected = true;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI} with Result {isConnected}");
        }
        catch { isConnected = false; }

    }

    void IAhsokaClientSocket.Disconnect()
    {
        if (isConnected)
        {
            try
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {endPoint.ServiceConfig.ServiceName}");

                socket.Disconnect(endPoint.ServiceConfig.DataURI);
                socketPoller.StopAsync();
                socketPoller.RemoveAndDispose(sendQueue);
                socketPoller.RemoveAndDispose(socket);
                socketPoller.Dispose();
                socketPoller = null;

                notificationSocket.Disconnect(endPoint.ServiceConfig.DataURI);
                notificationPoller.StopAsync();
                notificationPoller.RemoveAndDispose(notificationSocket);
                notificationPoller.Dispose();
                notificationPoller = null;

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");
            }
            catch { }
            finally { isConnected = false; }
        }
    }

    void IAhsokaClientSocket.SendMessage(AhsokaClientMessage socketMessage)
    {
        sendQueue.Enqueue(socketMessage);
    }

    AhsokaClientMessage RecieveMessage(DealerSocket recieveSocket)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client Message on {endPoint.ServiceConfig.ServiceName} ");
        AhsokaClientMessage returnMessage = null;
        NetMQMessage message = new();
        try
        {
            if (recieveSocket != null && recieveSocket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(1000), ref message, 2))
            {
                // Decode Request Header
                using MemoryStream headerData = new(message.Pop().Buffer);
                returnMessage = new()
                {
                    Header = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(headerData),
                    MessageData = message.Pop().Buffer
                };
            }
        }
        catch (SocketException) { } // Catching Errors During Shutdown / Disconnect

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Received Client Message on {endPoint.ServiceConfig.ServiceName} ");

        return returnMessage;
    }

}
