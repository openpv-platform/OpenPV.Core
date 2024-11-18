using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.ServiceFramework;

internal sealed class TCPServiceSocket : IAhsokaServiceSocket
{
    // While clients use two dealer sockets, we use a single
    // router socket that can reply / send to either one.
    RouterSocket socket;
    int socketThreadId = -1;
    IAhsokaEndPoint endPoint;

    // The Poller manages our thread for the main Socket (Router)
    // the send queue ensures that we don't send messages on the socket
    // from multiple threads.  Unlike C++ we don't need a second thread here since 
    // the send queue acts as our "Pair Socket" handling messages in mutliple threads.
    NetMQPoller socketPoller = new();
    NetMQQueue<NetMQMessage> sendQueue = new();
    List<byte[]> validClientIds = new ();
    
    public bool IsConnected { get; private set; }

    public TCPServiceSocket() { }

    void IAhsokaSocket.Connect(IAhsokaEndPoint endPoint)
    {
        try
        {
            this.endPoint = endPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI}");

            socket = new RouterSocket();
            socket.Options.Linger = TimeSpan.FromSeconds(1);
            socket.Bind(endPoint.ServiceConfig.DataListenURI);
            socketPoller.Add(socket);
            socketPoller.Add(sendQueue);
            IsConnected = true;
           
            socket.ReceiveReady += (o, e) =>
            {
                if (socketThreadId == -1) 
                    socketThreadId = Environment.CurrentManagedThreadId;

                endPoint.ReceiveMessage(false); // Services Don't Care Really
            };

            sendQueue.ReceiveReady += (o, e) =>
            {
                while (sendQueue.Count > 0)
                    socket.TrySendMultipartMessage(sendQueue.Dequeue());
            };

            socketPoller.RunAsync($"{endPoint.ServiceConfig.ServiceName}_Handler");

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI} with Result {IsConnected}");

        }
        catch { IsConnected = false; }
    }

    void IAhsokaSocket.Disconnect()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {endPoint.ServiceConfig.ServiceName}");

        if (IsConnected)
        {
            socket.Unbind(endPoint.ServiceConfig.DataListenURI);
            validClientIds.Clear();
        }

        IsConnected = false;

        if (socketPoller.IsRunning)
            socketPoller.StopAsync();

        socketPoller.RemoveAndDispose(socket);
        socketPoller.RemoveAndDispose(sendQueue);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");

    }

    void IAhsokaServiceSocket.RegisterClient(ClientRegistration registration)
    {
        validClientIds.Add(registration.ClientId);
    }

    void IAhsokaServiceSocket.DeregisterClient(ClientRegistration registration)
    {
        var indexToRemove = validClientIds.FindIndex(a => a.SequenceEqual(registration.ClientId));
        if (indexToRemove >= 0)
            validClientIds.RemoveAt(indexToRemove);
        
    }

    void IAhsokaSocket.SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        if (clientId == null && !sendAsNotification)
            throw new ApplicationException("Services must include client id when sending messages");

        IEnumerable<byte[]> clients = sendAsNotification ? 
                                    validClientIds : 
                                    new List<byte[]>() { clientId }; 

        foreach (var client in clients)
        {
            NetMQMessage message = new();

            message.Append(client);
            message.Append(header);
            message.Append(data);

            if (sendAsNotification)
            {
                // Send if In Recieve Thread...else Queue
                if (socketThreadId == Environment.CurrentManagedThreadId)
                    socket.TrySendMultipartMessage(message);
                else
                    sendQueue.Enqueue(message);
            }
            else
            {
                if (socketThreadId != Environment.CurrentManagedThreadId)
                    throw new ApplicationException("Service Replies must be sent from the Recieving Thread.");
                else
                    socket.TrySendMultipartMessage(message);
            }
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool isNotification)
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
