using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Ahsoka.ServiceFramework;

#region Client Socket 
internal sealed class TCPClientSocket : IAhsokaSocket
{
    IAhsokaEndPoint endPoint;

    NetMQPoller SocketPoller { get; set; }
    DealerSocket Socket { get; set; }

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
            Socket.Options.Identity = endPoint.GetClientId();

            Socket.ReceiveReady += (o, e) =>
            {
                ((IAhsokaClientEndpoint)endPoint).ClientDataReady();
            };


            Socket.Connect(this.endPoint.ServiceConfig.DataURI);
            SocketPoller.Add(Socket);
            SocketPoller.RunAsync($"{this.endPoint.ServiceConfig.ServiceName}_ClientListener");
         
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
                SocketPoller.StopAsync();
                SocketPoller.RemoveAndDispose(Socket);

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

        lock (Socket)
            Socket.SendMultipartMessage(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client {(sendAsNotification ? "Notification" : "Message")} Completed from {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs)
    {
        if (timeOutInMs > IAhsokaSocket.MaxTimeoutValue)
            timeOutInMs = IAhsokaSocket.MaxTimeoutValue;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} with Timeout {timeOutInMs}");

        header = data = clientId = null;

        NetMQMessage message = new();
        try
        {
            if (Socket != null && Socket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(timeOutInMs), ref message, 2))
            {
                header = message.Pop().Buffer;
                data = message.Pop().Buffer;
            }
        }
        catch (SocketException) { } // Catching Errors During Shutdown / Disconnect

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Received Client {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }

}
#endregion

#region Service Socket 
internal sealed class TCPServiceSocket : IAhsokaSocket
{
    IAhsokaEndPoint endPoint;

    List<Guid> validClientIds = new List<Guid>();
    NetMQPoller SocketPoller { get; set; }
    RouterSocket Socket { get; set; }
    NetMQMonitor SocketMonitor { get; set; }

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

            SocketMonitor = new NetMQMonitor(Socket, $"inproc://{Guid.NewGuid()}.inproc", SocketEvents.All);
            SocketMonitor.AttachToPoller(SocketPoller);

            IsConnected = true;

            Socket.ReceiveReady += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)endPoint).ServiceDataReady();
            };

            SocketMonitor.Accepted += (o, e) =>
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
            SocketMonitor.DetachFromPoller();
            Socket.Unbind(endPoint.ServiceConfig.DataListenURI);
            validClientIds.Clear();
        }

        IsConnected = false;

        SocketPoller.StopAsync();
        SocketPoller.RemoveAndDispose(Socket);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");

    }

    void IAhsokaSocket.SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        if (clientId == null && !sendAsNotification)
            throw new ApplicationException("Services must include client id when sending messages");

        NetMQSocket socket = Socket;

        IEnumerable<Guid> clients = sendAsNotification ? validClientIds : new List<Guid>() { new Guid(clientId) };

        NetMQMessage message = new();

        foreach (var client in clients)
        {
            message.Append(client.ToByteArray());
            message.Append(header);
            message.Append(data);

            lock (socket) // Protect Socket
                socket.TrySendMultipartMessage(message);
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs)
    {
        if (timeOutInMs > IAhsokaSocket.MaxTimeoutValue)
            timeOutInMs = IAhsokaSocket.MaxTimeoutValue;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Service {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        NetMQSocket socket = Socket;

        NetMQMessage message = new();
        if (socket.TryReceiveMultipartMessage(ref message, 3))
        {
            clientId = message.Pop().Buffer;
            header = message.Pop().Buffer;
            data = message.Pop().Buffer;

            var clientGuid = new Guid(clientId);
            if (!validClientIds.Contains(clientGuid))
                validClientIds.Add(clientGuid);
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Recieved Service {(receiveAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        return header != null;
    }
}
#endregion
