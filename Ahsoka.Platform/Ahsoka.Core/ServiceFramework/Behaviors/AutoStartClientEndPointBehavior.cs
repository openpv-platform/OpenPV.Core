using Ahsoka.Dispatch;
using Ahsoka.System;
using System;

namespace Ahsoka.ServiceFramework;

internal interface IAutoStartServiceClient : ICanAutoStart
{
    IAhsokaClientEndPoint Client { get; }
}

internal class AutoStartClientEndPointBehavior<TTransportIds> : IClientEndPointBehavior<TTransportIds>, IAutoStartServiceClient where TTransportIds : Enum, new()
{
    #region AutoStart
    IAhsokaClientEndPoint endPoint;
    object syncRoot = new();
    bool autoStartUsed = false;
    bool autoStartEnabled = false;

    IAhsokaClientEndPoint IAutoStartServiceClient.Client => endPoint;

    public void Init(AhsokaClientBase<TTransportIds> client, ServiceConfiguration config)
    {
        this.endPoint = client;

        if (config.Behaviors.HasFlag(EndPointBehaviors.AutoStart))
            autoStartEnabled = true;
    }

    public void OnSendMessage(TTransportIds transportId, object requestObject)
    {
        // Try AutoStart
        lock (syncRoot)
        {
            if (endPoint.CurrentRuntime == null && autoStartEnabled)
                ((ICanAutoStart)this).AutoStart(this);
        }
    }


    void ICanAutoStart.AutoStop(object requester)
    {
        // Auto Start was used, we will use 
        if (autoStartUsed)
        {
            endPoint.CurrentRuntime.RequestShutdown();
            endPoint.CurrentRuntime.WaitForShutdown();
        }
    }

    void ICanAutoStart.AutoStart(object requester)
    {
        // Capture if Auto Start is used for Auto Shutdown
        lock (syncRoot)
            if (endPoint.CurrentRuntime == null)
            {
                autoStartUsed = true;
                StartInternalService();
            }
    }

    private void StartInternalService()
    {
        lock (syncRoot)
        {
            if (endPoint.CurrentRuntime != null)
                return;

            var builder = AhsokaRuntime.CreateBuilder()
                                       .AddClients(endPoint);

            if (AhsokaServiceBase<TTransportIds>.IsServiceStarted)
            {
                builder.StartLocalRuntime();
            }
            else
            {
                builder.StartWithInternalServices();
            }
        }
    }

    public void OnNotificationMessageRecieved(TTransportIds transportId, object requestObject)
    {
        // Not Used
    }



    #endregion

}
