using Ahsoka.Core.Utility;
using System;
using System.Threading;

namespace Ahsoka.Core;

#pragma warning disable CS1591

public interface IAhsokaEndPoint
{
    ServiceConfiguration ServiceConfig { get; }

    AhsokaMessagesBase ServiceMessageInfo { get; }

    AhsokaRuntime CurrentRuntime { get; set; }

    bool IsStarted { get; }

    void Start();

    void Stop();

    uint EndPointId { get; }

    void WaitForStop();

    WaitHandle GetWaitHandle();
}

public abstract class AhsokaEndPoint<TTransportIds> : IAhsokaEndPoint
    where TTransportIds : Enum
{
    #region Fields
    uint endpointId = 0;
    readonly AhsokaMessagesBase serviceMessageInfo;
    readonly ManualResetEventSlim shutdownWaiter = new(false);
    #endregion

    #region Properties
    uint IAhsokaEndPoint.EndPointId { get => endpointId; }
    AhsokaRuntime IAhsokaEndPoint.CurrentRuntime { get; set; }
    public ServiceConfiguration ServiceConfig { get; protected set; }
    public AhsokaMessagesBase ServiceMessageInfo => serviceMessageInfo;

    #endregion

    public AhsokaEndPoint(ServiceConfiguration config, AhsokaMessagesBase messages)
    {
        this.endpointId = (uint)HashCode32.GetHashCode32(config.ServiceName);

        ServiceConfig = config;
        serviceMessageInfo = messages;

        // Windows can only host on the same box due to firewall rules
        if (SystemInfo.CurrentPlatform == PlatformFamily.Windows64)
            config.TcpListenAddress = config.TcpListenAddress.Replace("*", "localhost");
    }

    public void WaitForStop()
    {
        shutdownWaiter.Wait();
    }

    public WaitHandle GetWaitHandle()
    {
        return shutdownWaiter.WaitHandle;
    }

    protected virtual void OnEndPointStarting()
    {
    }

    protected virtual void OnEndPointStopped()
    {
        shutdownWaiter.Set();
    }

    protected virtual void OnEndPointStarted()
    {
    }

    abstract public bool IsStarted { get; }

    abstract public void Start();

    abstract public void Stop();

}


#pragma warning restore CS1591