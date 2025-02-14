using NetMQ;
using NetMQ.Sockets;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Core;

/// <summary>
/// Service Socket Handling TCP Connections
/// </summary>
public sealed class TCPServiceSocket : IAhsokaServiceSocket
{
    // While clients use two dealer sockets, we use a single
    // router socket that can reply / send to either one.
    Task socketTask;
    RouterSocket socket;
    int socketThreadId = -1;
    IAhsokaServiceEndPoint endPoint;

    // The Poller manages our thread for the main Socket (Router)
    // the send queue ensures that we don't send messages on the socket
    // from multiple threads.  Unlike C++ we don't need a second thread here since 
    // the send queue acts as our "Pair Socket" handling messages in mutliple threads.
    NetMQPoller socketPoller;
    NetMQQueue<NetMQMessage> sendQueue;

    bool isConnected = false;

    /// <summary>
    /// TCPServiceSocket
    /// </summary>
    public TCPServiceSocket() { }

    void IAhsokaServiceSocket.Connect(IAhsokaServiceEndPoint endPoint)
    {
        try
        {
            this.endPoint = endPoint as IAhsokaServiceEndPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI}");

            socketTask = Task.Run(() =>
            {
                using (socket = new())
                using (socketPoller = new())
                using (sendQueue = new())
                {
                    socket.Options.Linger = TimeSpan.FromSeconds(1);
                    socket.Bind(endPoint.ServiceConfig.DataListenURI);
                    socketPoller.Add(socket);
                    socketPoller.Add(sendQueue);
                    isConnected = true;

                    socket.ReceiveReady += (o, e) =>
                    {
                        if (socketThreadId == -1)
                            socketThreadId = Environment.CurrentManagedThreadId;

                        // Receive Message Data
                        ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId);

                        // Decode Request Header
                        using MemoryStream headerData = new(header);

                        var message = new AhsokaServiceMessage()
                        {
                            Header = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(headerData),
                            MessageData = data
                        };

                        if (clientId != null)
                            message.Header.ClientId = clientId;

                        this.endPoint.HandleServiceRequest(this, message);
                    };

                    sendQueue.ReceiveReady += (o, e) =>
                    {
                        while (sendQueue.Count > 0)
                            try { socket.TrySendMultipartMessage(sendQueue.Dequeue()); } catch { }
                    };

                    Thread.CurrentThread.Name = $"{endPoint.ServiceConfig.ServiceName}_Handler";
                    
                    // Run and Wait for Shutdown.
                    socketPoller.Run();

                    socket.Unbind(endPoint.ServiceConfig.DataListenURI);
                    socketPoller.Remove(socket);
                    socketPoller.Remove(sendQueue);
                }
            });

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI} with Result {isConnected}");

        }
        catch { isConnected = false; }
    }

    void IAhsokaServiceSocket.Disconnect()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {endPoint.ServiceConfig.ServiceName}");
        if (isConnected)
        {
            try
            {
                // Stop and Wait for Shutdown
                socketPoller?.Stop();
                socketTask?.Wait();
                socketTask = null;
                socketPoller = null;
            }
            catch { }
            finally { isConnected = false; }
        }
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");
    }

    void IAhsokaServiceSocket.SendMessage(AhsokaServiceMessage socketMessage)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service Message on {endPoint.ServiceConfig.ServiceName} ");

        // Prepare Response Message 
        using MemoryStream headerStream = new();
        ProtoBuf.Serializer.Serialize(headerStream, socketMessage.Header);

        NetMQMessage message = new();
        message.Append(socketMessage.ClientId);
        message.Append(headerStream.ToArray());
        message.Append(socketMessage.MessageData);

        // If Service Thread, send now, otherwise enqueue for send.
        if (socketThreadId == Environment.CurrentManagedThreadId)
            try { socket.TrySendMultipartMessage(message); } catch { }
        else if (socketMessage.IsNotification)
            sendQueue.Enqueue(message);
        else
            throw new ApplicationException("Reply Messages must be sent from Receiving Thread"); // Reply on separate thread


        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service Message on {endPoint.ServiceConfig.ServiceName} ");
    }

    bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Service Message on {endPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        NetMQMessage message = new();

        // Note that Services use a Single Socket But we lock here to prevent sending notifications
        // at the same time as we are receiving a message.
        if (socket.TryReceiveMultipartMessage(ref message, 3))
        {
            clientId = message.Pop().Buffer;
            header = message.Pop().Buffer;
            data = message.Pop().Buffer;
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Recieved Service Message on {endPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }
}
