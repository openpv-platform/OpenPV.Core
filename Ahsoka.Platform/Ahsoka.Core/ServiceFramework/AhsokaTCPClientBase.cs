using Ahsoka.Dispatch;
using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public abstract class AhsokaTCPClientBase<TTransportIds> : AhsokaClientBase<TTransportIds, TCPClientSocket>,
    ICanAutoStart
    where TTransportIds : Enum
{
    #region Fields
    bool autoStartUsed = false;
    #endregion

    #region Properties
    /// <summary>
    /// Indicates if the Client should start the Internal Service if its not started on first call.
    /// </summary>
    public static bool AutoStartEnabled { get; set; } = true;
    #endregion

    #region Methods
    protected AhsokaTCPClientBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {
        if (AutoStartEnabled)
            Dispatcher.Default.AddAutoStartInternal(this);
    }

    private void TryAutoStart()
    {
        lock(Socket) 
        {
            if (CurrentRuntime == null && AutoStartEnabled)
                ((ICanAutoStart)this).AutoStart(this);
        }
    }

    public override T SendNotificationWithResponse<T>(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue) where T : class
    {
        TryAutoStart();

        return base.SendNotificationWithResponse<T>(transportId, requestObject, timeoutInMs);
    }

    /// <summary>
    /// Starts a Service for the 
    /// </summary>
    /// <returns></returns>
    public void StartInternalService()
    {
        lock (Socket)
        {
            if (this.CurrentRuntime != null)
                return;

            var builder = AhsokaRuntime.CreateBuilder()
                .AddClients(this);

            if (AhsokaTCPServiceBase<TTransportIds>.IsServiceStarted)
            {
                builder.StartLocalRuntime();
            }
            else
            {
                builder.StartWithInternalServices();
            }
        }
    }

    void ICanAutoStart.AutoStop(object requester)
    {
        // Auto Start was used, we will use 
        if (autoStartUsed)
        {
            this.CurrentRuntime.RequestShutdown();
            this.CurrentRuntime.WaitForShutdown();
        }
    }

    void ICanAutoStart.AutoStart(object requester)
    {
        // Capture if Auto Start is used for Auto Shutdown
        lock (Socket)
            if (this.CurrentRuntime == null)
            {
                autoStartUsed = true;
                StartInternalService();
            }
    }
    #endregion
}
#pragma warning restore CS1591