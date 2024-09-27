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

    IAhsokaEndPoint endPoint;
    object socketSyncRoot = new(); // Protects Socket Send/Receive
    NetMQPoller socketPoller = new();
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
            IsConnected = true;
           
            socket.ReceiveReady += (o, e) =>
            {
                endPoint.ReceiveMessage(false); // Services Don't Care Really
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

        NetMQMessage message = new();

        foreach (var client in clients)
        {
            message.Append(client);
            message.Append(header);
            message.Append(data);

            lock (socketSyncRoot) // Protect Socket Recieves and Sends (mostly for Notifications)
                socket.TrySendMultipartMessage(message);

            message.Clear();
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
        lock (socketSyncRoot)
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
