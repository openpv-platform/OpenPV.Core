using Ahsoka.System;
using Ahsoka.Utility;
using NetMQ;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Central Runtime Engine for Ahsoka Services and Clients.
/// The runtime is responsible for managing the various threads and endpoints needed for client
/// or service implementations.  Typically there are two runtimes in a system...one for the Clients 
/// which will be started by the developer interacting with Ahsoka Services and a second for the Services
/// to run async from the clients.   Not that depending on the methods used in building the runtime, the 
/// services may run in the same process a different process or even on a separate target via TCPIP
/// 
/// Note: Creating Runtimes is accomplished by the RuntimeBuilder object.
/// </summary>
public partial class AhsokaRuntime
{
    #region Fields
    private const int MillisecondsTimeout = 5000;
    bool isRunning = false;
    readonly AutoResetEvent shutDownEvent = new(false);
    #endregion

    #region Properties
    internal Dictionary<string, IAhsokaEndPoint> EndPoints { get; init; } = new Dictionary<string, IAhsokaEndPoint>();
    #endregion

    #region Public Methods
    private AhsokaRuntime()
    {
    }

    /// <summary>
    /// Request that the runtime disconnects all endpoints and
    /// shuts down.
    /// </summary>
    public void RequestShutdown()
    {
        try
        {
            lock (syncRoot)
                if (isRunning)
                {
                    // Disconnect Blocks Until Threads End.
                    foreach (var item in EndPoints)
                    {
                        try
                        {
                            item.Value.Disconnect();
                        }
                        catch { }
                        finally
                        {
                            // Disconnect the Cache if this EndPoint is the Default Service Cache
                            if (IServiceCache.Default == item.Value)
                                IServiceCache.Default = null;
                        }
                    }

                    isRunning = false;
                }
        }
        finally
        {
            shutDownEvent.Set();
        }
    }

    /// <summary>
    /// Allows the consumer to wait for all services to complete shutting down.
    /// </summary>
    public void WaitForShutdown()
    {
        shutDownEvent.WaitOne();

        lock (syncRoot)
            currentRuntimes.Remove(this);
    }
    #endregion

    #region Private Methods
    private static AhsokaRuntime ExecuteRuntime(params IAhsokaEndPoint[] endPoints)
    {
        AhsokaRuntime runtime = new();
        foreach (var endpoint in endPoints.Where(x=>x != null))
        {
            endpoint.CurrentRuntime = runtime;
            runtime.EndPoints.Add(endpoint.ServiceConfig.ServiceName, endpoint);
        }

        lock (syncRoot)
            currentRuntimes.Add(runtime);

        runtime.ExecuteRuntime();

        return runtime;
    }

    private void ExecuteRuntime()
    {
        // Connect will start thread if needed to handle recieve (Services)
        foreach (var item in EndPoints)
            item.Value.Connect();

        isRunning = true;
    }

    /// <summary>
    /// Warmup Function allowing you to Pre-Load Libraries
    /// Since we do nothing but access protected methods and force libraries to load we should be ok.
    /// </summary>
    /// <returns></returns>
    public static void WarmupRuntime()
    {
        Task.Run(() =>
        {
            using var stopwatch = new AhsokaStopwatch("Ahsoka Warmup");
            var hardwareInfo = SystemInfo.HardwareInfo.FactoryInfo;
            var poller = new NetMQPoller();
        });
    }

    #endregion
}