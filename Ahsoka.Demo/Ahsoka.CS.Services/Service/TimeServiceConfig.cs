using Ahsoka.System;

namespace Ahsoka.Services.Demo.Service;

/// <summary>
///  Simple Configuration Class for Time Service
/// </summary>
internal static class TimeServiceConfig
{
    public static ServiceConfiguration Default { get; private set; }

    static TimeServiceConfig()
    {
        Default = new()
        {
            // Specifies the Name of the Service
            ServiceName = TimeService.Name,

            // Here we set the Configuration for the Primary Channel. This uses NetMQ's addressing scheme
            // and can be configured for inproc:\\ as you see here or for tcp by setting a
            // port like this.  Currently this 'connection' is managed with a NetMQ (0MQ) Dealer and Router Sockets
            //
            //      ListenAddress =     "tcp://*:5099"           // Listen on port 5099 used by Service
            //      ConnectionAddress = "tcp://localhost:5099"   // Connect to localhost:5099 used by Clients
            //
            // Here we set the notication ports.  This is used for async callbacks (Optional). 
            // If these addresses are specified, the service architecture will create a Publish / Subscribe
            // socket pair to enable async broadcasts (notifications) to the clients.
            //
            //      NotificationListenAddress =     "tcp://*:6099"           // Listen on port 6099 used by Service
            //      NotificationConnectionAddress = "tcp://localhost:6099"   // Connect to localhost:6099 used by Clients
            //
            DataChannel = 5099,
            NotificationChannel = 6099,
            TcpListenAddress = "localhost",
            TcpConnectionAddress = "localhost",
        };
    }
}
