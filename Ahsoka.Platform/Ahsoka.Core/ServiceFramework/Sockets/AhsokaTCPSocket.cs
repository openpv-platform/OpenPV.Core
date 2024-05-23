using Ahsoka.Utility;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

#region Client Socket 
internal sealed class TCPClientSocket : IAhsokaSocket
{
    IAhsokaEndPoint endPoint;

    CancellationTokenSource socketTokenSource = new();
    Task socketTask = null;
    DealerSocket socket;

    public bool IsConnected { get; private set; }

    public TCPClientSocket() { }

    void IAhsokaSocket.Connect(IAhsokaEndPoint endPoint)
    {
        try
        {
            this.endPoint = endPoint;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI}");
            
            socketTokenSource = new();
            socket = new DealerSocket();
            socket.Options.Linger = TimeSpan.FromSeconds(1);
            socket.Options.Identity = endPoint.GetClientId();
            socket.Connect(this.endPoint.ServiceConfig.DataURI);
            IsConnected = true;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Client {this.endPoint.ServiceConfig.ServiceName} @ {this.endPoint.ServiceConfig.DataURI} with Result {IsConnected}");

            socketTask = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Name = $"{endPoint.ServiceConfig.ServiceName}_ClientListener";
             
                while (!socketTokenSource.IsCancellationRequested)
                {
                    var eventResult = socket.Poll(PollEvents.PollIn | PollEvents.PollError, TimeSpan.FromSeconds(1));

                    if (eventResult == PollEvents.PollIn)
                        ((IAhsokaClientEndpoint)endPoint).ReceiveMessage();
                    else if (eventResult == PollEvents.PollError)
                        socketTokenSource.Cancel();
                }

            }, socketTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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

                socketTokenSource.Cancel();
                socketTask.Wait();
                socket.Dispose();


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

        lock (socket)
            socket.SendMultipartMessage(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client {(sendAsNotification ? "Notification" : "Message")} Completed from {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client Message on {endPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        NetMQMessage message = new();
        try
        {
            if (socket != null && socket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(1000), ref message, 2))
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
#endregion

#region Service Socket 
internal sealed class TCPServiceSocket : IAhsokaServiceSocket
{
    IAhsokaEndPoint endPoint;

    List<Guid> validClientIds = new ();
    Task socketTask;
    CancellationTokenSource socketTokenSource = new();
    RouterSocket socket;

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
            IsConnected = true;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Service {endPoint.ServiceConfig.ServiceName} @ {endPoint.ServiceConfig.DataListenURI} with Result {IsConnected}");

            socketTokenSource = new();
            socketTask = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Name = $"{endPoint.ServiceConfig.ServiceName}_Handler";
               
                while (!socketTokenSource.IsCancellationRequested)
                {
                    var eventResult = socket.Poll(PollEvents.PollIn | PollEvents.PollError, TimeSpan.FromSeconds(1));
                    
                    if (eventResult == PollEvents.PollIn)
                        ((IAhsokaServiceEndpoint)endPoint).ReceiveMessage();
                    else if (eventResult == PollEvents.PollError)
                        socketTokenSource.Cancel();
                }

            }, socketTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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

        socketTokenSource.Cancel();
        socketTask.Wait();
        socket.Dispose();

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {endPoint.ServiceConfig.ServiceName}");

    }

    void IAhsokaServiceSocket.RegisterClient(ClientRegistration registration)
    {
        validClientIds.Add(new Guid(registration.ClientId));
    }

    void IAhsokaServiceSocket.DeregisterClient(ClientRegistration registration)
    {
        validClientIds.Remove(new Guid( registration.ClientId));
    }

    void IAhsokaSocket.SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");

        if (clientId == null && !sendAsNotification)
            throw new ApplicationException("Services must include client id when sending messages");

        IEnumerable<Guid> clients = sendAsNotification ? validClientIds : new List<Guid>() { new Guid(clientId) };

        NetMQMessage message = new();

        foreach (var client in clients)
        {
            message.Append(client.ToByteArray());
            message.Append(header);
            message.Append(data);

            lock (socket) // Protect Socket
                socket.TrySendMultipartMessage(message);

            message.Clear();
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service {(sendAsNotification ? "Notification" : "Message")} on {endPoint.ServiceConfig.ServiceName} ");
    }

    bool IAhsokaSocket.ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Service Message on {endPoint.ServiceConfig.ServiceName} ");

        header = data = clientId = null;

        NetMQMessage message = new();
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
#endregion
