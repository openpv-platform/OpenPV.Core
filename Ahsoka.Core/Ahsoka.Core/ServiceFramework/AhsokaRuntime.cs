using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.Extensions.Hosting.Systemd;
using ProtoBuf.Meta;
using Renci.SshNet.Messages.Transport;
using System;
using System.Collections.Generic;
using System.Linq;

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
    static readonly object syncRoot = new();
    #endregion

    #region Static Singleton Methods
    /// <summary>
    /// Default Runtime
    /// </summary>
    public static AhsokaRuntime Default { get; private set; } = new();

    /// <summary>
    /// Creates a builder object for constructing Ahsoka Runtimes.
    /// </summary>
    /// <returns></returns>
    public static AhsokaRuntimeBuilder CreateBuilder()
    {
        return new AhsokaRuntimeBuilder();
    }
    #endregion

    #region Properties
    private List<IAhsokaClientEndPoint> ClientEndPoints { get; init; } = new();
    internal List<IAhsokaServiceEndpoint> ServiceEndPoints { get; init; } = new();
    #endregion

    #region Private Constructor
    private AhsokaRuntime()
    {

    }
    #endregion

    #region Public Interface
    /// <summary>
    /// Start and Add the Endpoints to the Runtime 
    /// </summary>
    /// <param name="endPoints"></param>
    /// <returns></returns>
    public void ConnectEndPoints(IEnumerable<IAhsokaEndPoint> endPoints)
    {
        foreach (var endpoint in endPoints.Where(x => x != null))
        {
            endpoint.CurrentRuntime = Default;
            if (endpoint is IAhsokaClientEndPoint client)
                Default.ClientEndPoints.Add(client);
            else if (endpoint is IAhsokaServiceEndpoint service)
                Default.ServiceEndPoints.Add(service);

            endpoint.Connect();
        }

    }

    /// <summary>
    /// Start and Add the Endpoints to the Runtime 
    /// </summary>
    /// <param name="endPoints"></param>
    /// <returns></returns>
    public void ConnectEndPoints(params IAhsokaEndPoint[] endPoints)
    {
        ConnectEndPoints(endPoints.AsEnumerable());
    }

    /// <summary>
    /// Shutdown All Endpoints (usually before stopping an application)
    /// </summary>
    public void DisconnectAllEndPoints(bool shutdownClients = true, bool shutdownServices = true)
    {
        lock (syncRoot)
        {
            if (shutdownClients)
            {
                foreach (var endpoint in ClientEndPoints.ToArray())
                    endpoint.Disconnect();

                ClientEndPoints.Clear();
            }

            if (shutdownServices)
            {
                foreach (var endpoint in ServiceEndPoints.ToArray())
                    endpoint.Disconnect();

                ServiceEndPoints.Clear();
            }
        }
    }

    /// <summary>
    /// Request that the runtime disconnects all endpoints and
    /// shuts down.
    /// </summary>
    public void DisconnectEndPoints(params IAhsokaEndPoint[] endpoints)
    {
        DisconnectEndPoints(endpoints.AsEnumerable());
    }

    /// <summary>
    /// Request that the runtime disconnects all endpoints
    /// shuts down.
    /// </summary>
    public void DisconnectEndPoints(IEnumerable<IAhsokaEndPoint> endpoints)
    {
        lock (syncRoot)
        {
            // Disconnect Blocks Until Threads End.
            foreach (var item in endpoints)
            {
                try
                {
                    item.Disconnect();
                }
                catch { }
                finally
                {

                    if (item is IAhsokaClientEndPoint client)
                        ClientEndPoints.Remove(client);
                    else if (item is IAhsokaServiceEndpoint service)
                        ServiceEndPoints.Remove(service);

                    // Disconnect the Cache if this EndPoint is the Default Service Cache
                    if (IServiceCache.Default == item)
                        IServiceCache.Default = null;
                    if (IServiceEvent.Default == item)
                        IServiceEvent.Default = null;
                }
            }

        }
    }

    /// <summary>
    /// Request that the runtime disconnects all endpoints and
    /// shuts down.
    /// </summary>
    public void DisconnectEndPoints(AhsokaEndPointSet set)
    {
        DisconnectEndPoints(set.EndPoints.ToArray());
    }

    #endregion

    #region Utility Methods
    /// <summary>
    /// Creates a set of Service Endpoints from the Given Configuration
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<IAhsokaServiceEndpoint> CreateServicesFromConfiguration(RuntimeConfiguration config)
    {
        using var stopwatch = new AhsokaStopwatch();

        // Find Services
        var types = ClassLoader.GetItemsWithAttribute<AhsokaServiceAttribute>();
        stopwatch.ResetTiming("Service Objects Discovered");

        List<IAhsokaServiceEndpoint> services = new();
        foreach (var item in config.Services)
        {
            var type = types.FirstOrDefault(x => x.Key.ServiceName == item.ServiceName);
            if (type.Value == null)
                throw new ArgumentException($"Could not find service to match {item.ServiceName}");
            else
            {
                IAhsokaServiceEndpoint service = ClassLoader.CreateType(type.Value, item) as IAhsokaServiceEndpoint;
                services.Add(service);
                stopwatch.ResetTiming($"{service.ServiceConfig.ServiceName} Added To List");
            }
        }

        return services;
    }

    /// <summary>
    /// Releases startup to continue on Linux system
    /// </summary>
    public static void ReleaseStartup()
    {
        SystemdNotifier notifier = new();
        notifier.Notify(ServiceState.Ready);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Releasing Startup (System Notify)");
    }
    #endregion

}