using System;
using System.Threading;

namespace Ahsoka.ServiceFramework;
public partial class AhsokaRuntime
{
    /// <summary>
    /// Shutdown All Endpoint
    /// </summary>
    [Obsolete("Use Singleton (Default) DisconnectAllEndPoints Instead ")]
    public static void ShutdownAll()
    {
        Default.DisconnectAllEndPoints();
    }


    public partial class AhsokaRuntimeBuilder
    {
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
            Default.ConnectEndPoints(endPoints.ToArray());
            return new AhsokaEndPointSet(endPoints);
        }
    }

    public partial class AhsokaEndPointSet
    {
        ManualResetEventSlim shutdownEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// Obsolete Method for shutting down a set of endpoints
        /// </summary>
        [Obsolete("Use AhsokaRuntime.Default.DisconnectEndpoints() instead")]
        public void RequestShutdown()
        {
            AhsokaRuntime.Default.DisconnectEndPoints(this.endPoints.ToArray());
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