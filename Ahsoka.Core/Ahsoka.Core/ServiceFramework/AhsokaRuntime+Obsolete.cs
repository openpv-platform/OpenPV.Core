using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.ServiceModel.Description;
using System.Threading;

#pragma warning disable CS0618 
namespace Ahsoka.ServiceFramework;

[ExcludeFromCodeCoverage]
public partial class AhsokaRuntime
{

    /// <summary>
    /// Releases startup to continue on Linux system
    /// </summary>
    [Obsolete("Use ApplicationContext.ReleaseStartup Instead")]
    public static void ReleaseStartup()
    {
        ApplicationContext.ReleaseStartup();
    }

    /// <summary>
    /// Creates a builder object for constructing Ahsoka Runtimes.
    /// </summary>
    /// <returns></returns>
    public static AhsokaRuntimeBuilder CreateBuilder()
    {
        return new AhsokaRuntimeBuilder();
    }

    /// <summary>
    /// Shutdown All Endpoint
    /// </summary>
    [Obsolete("Use Singleton (Default) DisconnectAllEndPoints Instead ")]
    public static void ShutdownAll()
    {
        Default.StopAllEndPoints();
    }

    /// <summary>
    /// Builder Class for Ahsoka Runtimes - Obsolte
    /// </summary>
    [Obsolete ("Use AhsokaRuntime Startup Functions or use AhsokaEndpoints directly via Connect/Disconnect")]
    public partial class AhsokaRuntimeBuilder
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
        public AhsokaRuntimeBuilder AddServices(params IAhsokaServiceEndPoint[] endPoints)
        {
            if (this.endPoints.Count > 0 && isClient)
                throw new ArgumentException("Client and Service Endpoints can not be added to the same runtime.");

            isClient = false;
            this.endPoints.AddRange(endPoints);

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
        /// <summary>
        /// Finishes the build process and starts the runtime with the configured services
        /// Use AddClients or AddServices to use this method. 
        /// Note: This method starts ONLY the endpoints you have added and is used in more advanced topographies.
        /// </summary>
        /// <returns>AhsokaRuntime in a running state</returns>
        /// <exception cref="ArgumentException"></exception>
        [Obsolete("Use AhsokaRuntime.Default.ConnectEndPoints Instead")]
        public AhsokaEndPointSet StartLocalRuntime()
        {
            Default.StartEndPoints(endPoints.ToArray());
            return new AhsokaEndPointSet(endPoints);
        }


        /// <summary>
        /// This method completes the build and starts a client and service runtime for the configured
        /// Client Endpoints.  Note that this can ONLY be used for Clients as the service runtime is generated
        /// automatically to match the configured clients.
        /// Note: This is the recommended / simpliest method of starting the Ahsoka Runtime System.
        /// </summary>
        /// <returns>AhsokaRuntime in a running state</returns>
        /// <exception cref="ArgumentException"></exception>
        [Obsolete("Use AhsokaRuntime.Default.ConnectEndPoints Instead. If the list of service runtimes is needed, use AhsokaRuntime.CreateServicesFromConfiguration()")]
        public AhsokaEndPointSet StartWithInternalServices()
        {
            using var stopwatch = new AhsokaStopwatch("StartWithInternalServices");

            if (!isClient)
                throw new ArgumentException("Must specify Client Endpoints for using StartWithInternalServices");

            stopwatch.ResetTiming("Services Created");

            // Start Clients...Force Auto Start 
            foreach (var item in endPoints)
                item.ServiceConfig.Behaviors |= System.EndPointBehaviors.AutoStart;

            Default.StartEndPoints(endPoints.ToArray());

            return new AhsokaEndPointSet(endPoints);
        }

        /// <summary>
        /// This method completes the build and starts a client and service runtime for the configured
        /// Client Endpoints.  Note that this can ONLY be used for Clients as the service runtime is generated
        /// automatically to match the configured clients. In addition this method requires that the SetServiceExecutable
        /// method was called since the call will start an external process to handle the services.
        /// </summary>
        /// <returns>AhsokaRuntime in a running state</returns>
        /// <exception cref="ArgumentException"></exception>
        [Obsolete("Use AhsokaRuntime.StartExternalRuntime Instead")]
        public AhsokaEndPointSet StartWithExternalServices()
        {
            if (!isClient)
                throw new ArgumentException("Must specify Client Endpoints for using StartWithInternalServices");

            if (!File.Exists(serviceExecutable))
                throw new ArgumentException("Service Executable not specified. Use SetServiceExecutable to reference an application that accepts --StartServices <RuntimeConfigurationFile> as parameters.");

            string startupFile = CreateServiceConfiguration(this.endPoints);

            // Remove AutoStart
            foreach (var item in endPoints)
                item.ServiceConfig.Behaviors &= System.EndPointBehaviors.AutoStart;

            StartProcessExecutable(serviceExecutable, startupFile);

            Default.StartEndPoints(endPoints.ToArray());

            return new AhsokaEndPointSet(endPoints);
        }
    }

    /// <summary>
    /// Request that the runtime disconnects all endpoints and
    /// shuts down.
    /// </summary>
    [Obsolete("Use DisconnectEndPoints<IEnumerable<IAhsokaEndPoint>() or Connect/Disconnect")]
    public void DisconnectEndPoints(AhsokaEndPointSet set)
    {
        StopEndPoints(set.EndPoints);
    }


    /// <summary>
    /// Set of Endpoints To support Obsolete Calls
    /// </summary>
    /// 
    public partial class AhsokaEndPointSet
    {
        ManualResetEventSlim shutdownEvent = new ManualResetEventSlim(false);
        readonly List<IAhsokaEndPoint> endPoints = new();

        internal IEnumerable<IAhsokaEndPoint> EndPoints => endPoints;

        internal AhsokaEndPointSet(IEnumerable<IAhsokaEndPoint> endPoints)
        {
            this.endPoints.AddRange(endPoints);
        }

        /// <summary>
        /// Obsolete Method for shutting down a set of endpoints
        /// </summary>
        [Obsolete("Use AhsokaRuntime.Default.DisconnectEndpoints() instead")]
        public void RequestShutdown()
        {
            AhsokaRuntime.Default.StopEndPoints(this.endPoints.ToArray());
            shutdownEvent.Set();
        }

        /// <summary>
        /// Obsolete Method for shutting down a set of endpoints
        /// </summary>
        [Obsolete("Use AhsokaRuntime.Default.DisconnectEndpoints() instead.  If waiting on other services, wait for the the service / client to disconnect instead")]
        public void WaitForShutdown()
        {
            shutdownEvent.Wait();
        }

    }
}


/// <summary>
/// Set of Endpoints Started in the Runtime.
/// </summary>
public partial class AhsokaEndPointSet
{
    readonly List<IAhsokaEndPoint> endPoints = new();

    internal List<IAhsokaEndPoint> EndPoints => endPoints;

    internal AhsokaEndPointSet(IEnumerable<IAhsokaEndPoint> endPoints)
    {
        this.endPoints.AddRange(endPoints);
    }
}
