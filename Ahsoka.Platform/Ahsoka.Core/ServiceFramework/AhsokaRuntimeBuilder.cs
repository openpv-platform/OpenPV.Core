using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.Extensions.Hosting.Systemd;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

public partial class AhsokaRuntime
{
    #region Builder
    /// <summary>
    /// Creates a builder object for constructing Ahsoka Runtimes.
    /// </summary>
    /// <returns></returns>
    public static AhsokaRuntimeBuilder CreateBuilder()
    {
        return new AhsokaRuntimeBuilder();
    }

    /// <summary>
    /// Builder object for specifying configurations and behaviors
    /// for Runtime
    /// </summary>
    public class AhsokaRuntimeBuilder
    {
        #region Fields
        bool isClient = false;
        string serviceExecutable = string.Empty;
        readonly List<IAhsokaEndPoint> endPoints = new();
        #endregion

        #region Builder Methods
        internal AhsokaRuntimeBuilder()
        {

        }

        /// <summary>
        /// Add a collection of Client endpoints to the runtime. These endpoints will be 
        /// managed by the runtime and shutdown removed / disconnected when the runtime is shutdown
        /// Note: Service and Client Endpoint types can't be mixed in a single runtime.
        /// </summary>
        /// <param name="endPoints">Array of client runtimes</param>
        /// <returns>AhsokaRuntimeBuilder to continue building until a Start Method is used.</returns>
        /// <exception cref="ArgumentException"></exception>
        public AhsokaRuntimeBuilder AddClients(params IAhsokaClientEndPoint[] endPoints)
        {
            if (this.endPoints.Count > 0 && !isClient)
                throw new ArgumentException("Client and Service Endpoints can not be added to the same runtime.");

            isClient = true;
            this.endPoints.AddRange(endPoints);

            return this;
        }

        /// <summary>
        /// Add a collection of Service  endpoints to the runtime. These endpoints will be 
        /// managed by the runtime and shutdown removed / disconnected when the runtime is shutdown
        /// Note: Service and Client Endpoint types can't be mixed in a single runtime.        /// </summary>
        /// <param name="endPoints">Array of service runtimes</param>
        /// <returns>AhsokaRuntimeBuilder to continue building until a Start Method is used.</returns>
        /// <exception cref="ArgumentException"></exception>
        public AhsokaRuntimeBuilder AddServices(params IAhsokaServiceEndpoint[] endPoints)
        {
            if (this.endPoints.Count > 0 && isClient)
                throw new ArgumentException("Client and Service Endpoints can not be added to the same runtime.");

            isClient = false;
            this.endPoints.AddRange(endPoints);

            return this;
        }

        /// <summary>
        /// Generate a set of services from a runtime configuration.  This allows a developer to start a set of services
        /// directly from a RuntimeConfiguration.   This is useful if you already have a configuration file or set of clients
        /// and just want to start the equivelent service endpoints.
        /// </summary>
        /// <param name="configuration">RuntimeConfiguration which was created in code or from a configuration file.</param>
        /// <returns>AhsokaRuntimeBuilder to continue building until a Start Method is used.</returns>
        public AhsokaRuntimeBuilder AddServicesFromConfiguration(RuntimeConfiguration configuration)
        {
            endPoints.AddRange(CreateServicesFromConfiguration(configuration));
            return this;
        }

        /// <summary>
        /// Allows the developer to specify the path to a Ahsoka.CommandLine to use for starting a set of services
        /// in an external process.
        /// </summary>
        /// <param name="fileName">Path to the Ahsoka.CommandLine executable or another executable that can take a RuntimeConfigutation file
        /// as a start up arguement.</param>
        /// <returns>AhsokaRuntimeBuilder to continue building until a Start Method is used.</returns>
        public AhsokaRuntimeBuilder SetServiceExecutable(string fileName)
        {
            serviceExecutable = fileName;
            return this;
        }
        #endregion

        #region Startup Methods

        /// <summary>
        /// Finishes the build process and starts the runtime with the configured services
        /// Use AddClients or AddServices to use this method. 
        /// Note: This method starts ONLY the endpoints you have added and is used in more advanced topographies.
        /// </summary>
        /// <returns>AhsokaRuntime in a running state</returns>
        /// <exception cref="ArgumentException"></exception>
        public AhsokaRuntime StartLocalRuntime()
        {
            return ExecuteRuntime(endPoints.ToArray());
        }

        /// <summary>
        /// This method completes the build and starts a client and service runtime for the configured
        /// Client Endpoints.  Note that this can ONLY be used for Clients as the service runtime is generated
        /// automatically to match the configured clients.
        /// Note: This is the recommended / simpliest method of starting the Ahsoka Runtime System.
        /// </summary>
        /// <returns>AhsokaRuntime in a running state</returns>
        /// <exception cref="ArgumentException"></exception>
        public AhsokaRuntime StartWithInternalServices()
        {
            using var stopwatch = new AhsokaStopwatch("StartWithInternalServices");

            if (!isClient)
                throw new ArgumentException("Must specify Client Endpoints for using StartWithInternalServices");

            List<IAhsokaServiceEndpoint> services = new();
            foreach (IAhsokaClientEndPoint item in endPoints.Cast<IAhsokaClientEndPoint>())
                services.Add(item.CreateDefaultService());

            stopwatch.ResetTiming("Services Created");

            // Start Services
            ExecuteRuntime(services.ToArray());

            return ExecuteRuntime(endPoints.ToArray());
        }

        /// <summary>
        /// This method completes the build and starts a client and service runtime for the configured
        /// Client Endpoints.  Note that this can ONLY be used for Clients as the service runtime is generated
        /// automatically to match the configured clients. In addition this method requires that the SetServiceExecutable
        /// method was called since the call will start an external process to handle the services.
        /// </summary>
        /// <returns>AhsokaRuntime in a running state</returns>
        /// <exception cref="ArgumentException"></exception>
        public AhsokaRuntime StartWithExternalServices()
        {
            if (!isClient)
                throw new ArgumentException("Must specify Client Endpoints for using StartWithInternalServices");

            if (!File.Exists(serviceExecutable))
                throw new ArgumentException("Service Executable not specified. Use SetServiceExecutable to reference an application that accepts --StartServices <RuntimeConfigurationFile> as parameters.");

            string startupFile = CreateServiceConfiguration();

            StartProcessExecutable(serviceExecutable, startupFile);

            return ExecuteRuntime(endPoints.ToArray());
        }
        #endregion

        #region Internal Methods
        private List<IAhsokaServiceEndpoint> CreateServicesFromConfiguration(RuntimeConfiguration config)
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

        private string CreateServiceConfiguration()
        {
            RuntimeConfiguration config = new();
            foreach (var item in endPoints)
                config.Services.Add(item.ServiceConfig);

            // Write out Protobuffer Config
            string startupFile = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName() + ".pbuff";
            using var stream = new FileStream(startupFile, FileMode.OpenOrCreate);
            ProtoBuf.Serializer.Serialize(stream, config);
            
            return startupFile;
        }

        private void StartProcessExecutable(string executablePath, string configurationFile)
        {
            // Give Process Execute Rights
            if (!OperatingSystem.IsWindows())
                Process.Start("chmod", "+x " + executablePath).WaitForExit();

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                if (!String.IsNullOrEmpty(info.Data))
                {
                    if (!info.Error)
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "External Process Information: " + info.Data);
                    else
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "External Process Error: " + info.Data);
                        tokenSource.Cancel();
                    }
                }
            });

            Task.Run(() =>
            {
                ProcessUtility.RunProcessAsync(executablePath, $"--StartServices {configurationFile}", null, progressText, tokenSource.Token);
            },tokenSource.Token);
        }
        #endregion
    }
    #endregion

    #region Static Factory 
    static readonly object syncRoot = new();
    private readonly static List<AhsokaRuntime> currentRuntimes = new();

    /// <summary>
    /// Releases startup to continue on Linux system
    /// </summary>
    public static void ReleaseStartup()
    {
        SystemdNotifier notifier = new();
        notifier.Notify(ServiceState.Ready);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Releasing Startup (System Notify)");
    }

    /// <summary>
    /// 
    /// </summary>
    public static void ShutdownAll()
    {
        lock (syncRoot)
        {
            foreach (var runtime in currentRuntimes.ToArray())
            {
                runtime.RequestShutdown();
                runtime.WaitForShutdown();
            }
        }
    }
    #endregion
}

/// <summary>
/// Attribute used to Describe Services
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class AhsokaServiceAttribute : Attribute
{
    /// <summary>
    /// Name of the Service / Service Configuration
    /// </summary>
    public string ServiceName { get; init; }

    /// <summary>
    /// Constructor for Specifying Service
    /// </summary>
    /// <param name="serviceName"></param>
    public AhsokaServiceAttribute(string serviceName) { ServiceName = serviceName; }
}