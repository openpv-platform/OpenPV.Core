using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

internal sealed class LocalSocket : IAhsokaServiceSocket, IAhsokaClientSocket
{
    Task notifTask;
    CancellationTokenSource tokenSource;
    byte[] clientId;
    IAhsokaServiceEndPoint serviceEndPoint;
    IAhsokaClientEndPoint clientEndPoint;
    BlockingCollection<AhsokaClientMessage> notificationObjects = new();

    public void Connect(IAhsokaServiceEndPoint endPoint)
    {
        serviceEndPoint = endPoint;
    }

    public void Connect(IAhsokaClientEndPoint endPoint)
    {
        clientEndPoint = endPoint;

        clientId = endPoint.GetClientId();

        tokenSource = new();
        notifTask = Task.Run(() =>
        {
            AhsokaClientMessage response = null;
            while (!tokenSource.IsCancellationRequested)
            {
                try
                {
                    notificationObjects.TryTake(out response, 1000, tokenSource.Token);

                    if (response != null)
                        clientEndPoint.HandleNotification(response);
                }
                catch (OperationCanceledException) { }
            }
        });
    }

    public void Disconnect()
    {
        tokenSource.Cancel();
        notifTask.Wait();

        serviceEndPoint.RemoveLocalSocket(this);
    }

    // Handle Sending Messages to Clients
    public void SendMessage(AhsokaServiceMessage message)
    {
        if (message.IsNotification)
            notificationObjects.Add(new AhsokaClientMessage() { Header = message.Header, MessageData = message.MessageData });
        else
            clientEndPoint.HandleResponse(new AhsokaClientMessage() { Header = message.Header, MessageData = message.MessageData });
    }

    // Handle Sending Messages to Services
    public void SendMessage(AhsokaClientMessage message)
    {
        serviceEndPoint.HandleServiceRequest(this, new AhsokaServiceMessage() { Header = message.Header, MessageData = message.MessageData, ClientId = clientId, IsNotification = false });
    }
}
