using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IAhsokaEndPoint
{
    ServiceConfiguration ServiceConfig { get; }

    AhsokaRuntime CurrentRuntime { get; set; }

    bool IsStarted { get; }

    void Start();

    void Stop();

    int EndPointId { get; }

    void WaitForStop();

    WaitHandle GetWaitHandle();
}

public abstract class AhsokaEndPoint<TTransportIds> : IAhsokaEndPoint
    where TTransportIds : Enum
{
    #region Fields
    int endpointId = 0;
    readonly AhsokaMessagesBase serviceMessageInfo;
    readonly ManualResetEventSlim shutdownWaiter = new(false);
    #endregion

    #region Properties
    int IAhsokaEndPoint.EndPointId { get => endpointId; }
    AhsokaRuntime IAhsokaEndPoint.CurrentRuntime { get; set; }
    public ServiceConfiguration ServiceConfig { get; protected set; }
    public AhsokaMessagesBase ServiceMessageInfo => serviceMessageInfo;

    #endregion

    public AhsokaEndPoint(ServiceConfiguration config, AhsokaMessagesBase messages)
    {
        this.endpointId = HashCode32.GetHashCode32(config.ServiceName);

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