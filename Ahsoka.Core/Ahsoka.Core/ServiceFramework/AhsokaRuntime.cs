using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.Extensions.Hosting.Systemd;
using ProtoBuf.Meta;
using Renci.SshNet.Messages.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using static NetMQ.NetMQSelector;

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
    static Task externalServicesTask = null;
    static CancellationTokenSource tokenSource = null;
    #endregion

    #region Static Singleton Methods
    /// <summary>
    /// Default Runtime
    /// </summary>
    public static AhsokaRuntime Default { get; private set; } = new();
    #endregion

    #region Properties
    private List<IAhsokaClientEndPoint> ClientEndPoints { get; init; } = new();
    internal List<IAhsokaServiceEndPoint> ServiceEndPoints { get; init; } = new();
    #endregion

    #region Private Constructor
    private AhsokaRuntime()
    {

    }
    #endregion

    #region Public Interface
    /// <summary>
    /// Get a reference to a Service Available in the Runtime
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public IAhsokaServiceEndPoint GetService(string serviceName)
    {
        return ServiceEndPoints.Where(x=>x.ServiceConfig.ServiceName == serviceName).FirstOrDefault();
    }

    /// <summary>
    /// Get a reference to a Client Available in the Runtime
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public IAhsokaClientEndPoint GetClient(string serviceName)
    {
        return ClientEndPoints.Where(x => x.ServiceConfig.ServiceName == serviceName).FirstOrDefault();
    }

    /// <summary>
    /// Start and Add the Endpoints to the Runtime 
    /// </summary>
    /// <param name="endPoints"></param>
    /// <returns></returns>
    public void ConnectEndPoints(IEnumerable<IAhsokaEndPoint> endPoints)
    {
        foreach (var endpoint in endPoints.Where(x => x != null))
        {
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

            // Shutdown External Task if Running
            if (tokenSource != null && externalServicesTask != null)
            {
                tokenSource.Cancel();
                externalServicesTask.Wait();
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
            }

        }
    }
    #endregion

    #region Internal Methods
    internal void AddEndPointInternal(IAhsokaEndPoint ahsokaEndPoint)
    {
        if (ahsokaEndPoint is IAhsokaClientEndPoint client)
        {
            if (!ClientEndPoints.Contains(client))
                ClientEndPoints.Add(client);
        }
        else if (ahsokaEndPoint is IAhsokaServiceEndPoint service)
        {
            if (!ServiceEndPoints.Contains(service))
                ServiceEndPoints.Add(service);
        }
    }

    internal void RemoveEndPointInternal(IAhsokaEndPoint ahsokaEndPoint)
    {
        if (ahsokaEndPoint is IAhsokaClientEndPoint client)
        {
            if (!ClientEndPoints.Contains(client))
                ClientEndPoints.Remove(client);
        }
        else if (ahsokaEndPoint is IAhsokaServiceEndPoint service)
        {
            if (!ServiceEndPoints.Contains(service))
                ServiceEndPoints.Remove(service);
        }

        // Disconnect the Cache if this EndPoint is the Default Service Cache
        if (IServiceCache.Default == ahsokaEndPoint)
            IServiceCache.Default = null;
        if (IServiceEvent.Default == ahsokaEndPoint)
            IServiceEvent.Default = null;

    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Start an External Service for the Specified EndPoints
    /// </summary>
    /// <param name="executablePath"></param>
    /// <param name="endpoints"></param>
    public static void StartProcessExecutable(string executablePath, IEnumerable<IAhsokaEndPoint> endpoints)
    {
        string config = CreateServiceConfiguration(endpoints);
        StartProcessExecutable(executablePath, config);
    }

    /// <summary>
    /// Creates a set of Service Endpoints from the Given Configuration
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<IAhsokaServiceEndPoint> CreateServicesFromConfiguration(RuntimeConfiguration config)
    {
        using var stopwatch = new AhsokaStopwatch();

        // Find Services
        var types = ClassLoader.GetItemsWithAttribute<AhsokaServiceAttribute>();
        stopwatch.ResetTiming("Service Objects Discovered");

        List<IAhsokaServiceEndPoint> services = new();
        foreach (var item in config.Services)
        {
            var type = types.FirstOrDefault(x => x.Key.ServiceName == item.ServiceName);
            if (type.Value == null)
                throw new ArgumentException($"Could not find service to match {item.ServiceName}");
            else
            {
                IAhsokaServiceEndPoint service = ClassLoader.CreateType(type.Value, item) as IAhsokaServiceEndPoint;
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
    
    #region Internal Methods

    private static string CreateServiceConfiguration(IEnumerable<IAhsokaEndPoint> endpoints)
    {
        RuntimeConfiguration config = new();
        foreach (var item in endpoints)
            config.Services.Add(item.ServiceConfig);

        // Write out Protobuffer Config
        string startupFile = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName() + ".pbuff";
        using var stream = new FileStream(startupFile, FileMode.OpenOrCreate);
        ProtoBuf.Serializer.Serialize(stream, config);

        return startupFile;
    }

    private static void StartProcessExecutable(string executablePath, string configurationFile)
    {
        // Give Process Execute Rights
        if (!OperatingSystem.IsWindows())
            Process.Start("chmod", "+x " + executablePath).WaitForExit();

        tokenSource = new CancellationTokenSource();

        IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
        {
            if (!String.IsNullOrEmpty(info.Data))
            {
                if (!info.Error)
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, "External Process Information: " + info.Data);
                else
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, "External Process Error: " + info.Data);
                    tokenSource.Cancel();
                }
            }
        });

        externalServicesTask = Task.Run(() =>
        {
            ProcessUtility.RunProcessAsync(executablePath, $"--StartServices {configurationFile}", null, progressText, tokenSource.Token);

        },
        tokenSource.Token);
    }

    #endregion

}