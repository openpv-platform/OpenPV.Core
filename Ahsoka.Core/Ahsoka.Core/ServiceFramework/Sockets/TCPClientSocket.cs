using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;

namespace Ahsoka.ServiceFramework;

internal sealed class TCPClientSocket : IAhsokaClientSocket
{
    IAhsokaClientEndPoint endPoint;
    bool isConnected = false;

    // Main Socket Management
    NetMQPoller socketPoller = new();
    DealerSocket socket;
    NetMQQueue<AhsokaClientMessage> sendQueue = new();

    // Result Object
    BlockingCollection<AhsokaClientMessage> responseObjects = new();

    // Notification Handler
    NetMQPoller notificationPoller = new();
    DealerSocket notificationSocket;

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
            socketPoller.Add(sendQueue);

            sendQueue.ReceiveReady += (o, e) =>
            {
                while (sendQueue.Count > 0)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Client Message from {endPoint.ServiceConfig.ServiceName} ");

                    var socketMessage = sendQueue.Dequeue();

                    // Prepare Response Message 
                    using MemoryStream headerStream = new();
                    ProtoBuf.Serializer.Serialize(headerStream, socketMessage.Header);

                    NetMQMessage message = new();
                    message.Append(headerStream.ToArray());
                    message.Append(socketMessage.MessageData);

                    socket.TrySendMultipartMessage(message);

                    // If this message has a reply, we wait for it.
                    if (socketMessage.ResponseTimeout > 0)
                    {
                        socketMessage = ReceiveMessage(socket, socketMessage.ResponseTimeout);
                        responseObjects.Add(socketMessage);
                    }

                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client Message Completed from {endPoint.ServiceConfig.ServiceName} ");
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
                AhsokaClientMessage message = ReceiveMessage(notificationSocket);

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
                socket.Dispose();
              
                notificationSocket.Disconnect(endPoint.ServiceConfig.DataURI);
                notificationPoller.StopAsync();
                notificationPoller.RemoveAndDispose(notificationSocket);

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");
            }
            catch { }
            finally { isConnected = false; }
        }
    }


    /// <summary>
    /// Send Data over the Socket Transport
    /// </summary>
    /// <param name="socketMessage"></param>
    /// <param name="timeOutInMs"></param>
    AhsokaClientMessage IAhsokaClientSocket.SendMessageWithResponse(AhsokaClientMessage socketMessage, int timeOutInMs)
    {
        socketMessage.ResponseTimeout = timeOutInMs;
        sendQueue.Enqueue(socketMessage);

        var responseObject = responseObjects.Take();

        return responseObject;
    }

    /// <summary>
    /// Send Data over the Socket Transport
    /// </summary>
    /// <param name="socketMessage"></param>
    void IAhsokaClientSocket.SendMessage(AhsokaClientMessage socketMessage)
    {
        socketMessage.ResponseTimeout = -1;
        sendQueue.Enqueue(socketMessage);
    }

    AhsokaClientMessage ReceiveMessage(DealerSocket recieveSocket, int messageTimeoutInMs = 1000)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client Message on {endPoint.ServiceConfig.ServiceName} ");
        AhsokaClientMessage returnMessage = null;
        NetMQMessage message = new();
        try
        {
            if (recieveSocket != null && recieveSocket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(messageTimeoutInMs), ref message, 2))
            {
                // Decode Request Header
                using MemoryStream headerData = new(message.Pop().Buffer);
                returnMessage = new ()
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
