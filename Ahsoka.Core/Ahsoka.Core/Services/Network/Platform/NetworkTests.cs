using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Ahsoka.Services.Network.Platform;

[ExcludeFromCodeCoverage]
internal class NetworkTests
{
    static readonly AutoResetEvent enableComplete = new(false);
    static readonly AutoResetEvent connectComplete = new(false);
    static readonly AutoResetEvent disconnectComplete = new(false);

    public static void TestNetworkService()
    {
        // Start NetworkService
        Console.WriteLine($"Starting Runtimes");

        // Start client for NetworkService
        var client = new NetworkServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                    .AddClients(client)
                    .StartWithInternalServices();

        Console.WriteLine($"Getting Adapters");
        var networkAdapters = client.GetNetworkAdapters();
        Console.WriteLine($"Found {networkAdapters.Adapters.Count} Adapters");

        // Fetch Client Adapter
        var adapter = networkAdapters.Adapters.First(x => x.AdapterType == NetworkAdapterType.WifiClient);
        var adapterRequest = new AdapterRequest() { AdapterId = adapter.AdapterId };

        Console.WriteLine();
        Console.WriteLine($"Adapter: {adapter.Description}\r\n is of type {adapter.AdapterType} with AdapterId {adapter.AdapterId}");
        var config = client.GetNetworkConfiguration(adapterRequest);

        Console.WriteLine();
        Console.WriteLine($"Adapter is Configured for {adapter.AdapterType}");

        // Configure
        config.ClientConfiguration = new IPConfiguration() { ConnectionType = IPAddressConfiguration.Dhcp };

        // Set Configuration
        Console.WriteLine();
        Console.WriteLine($"Setting Configuration for {config.AdapterId}");
        client.SetNetworkConfiguration(config);

        // Enable Adapter
        Console.WriteLine();
        Console.WriteLine($"Enable Adapter {adapterRequest.AdapterId}");
        client.EnableAdapter(adapterRequest);

        // Wait for Enable Complete
        Console.WriteLine($"Waiting for Adapter To Be Ready");
        enableComplete.WaitOne();

        string key = "";

        while (key != "q")
        {
            Console.WriteLine();
            Console.WriteLine($"List Networks in Range");
            var networkList = client.DiscoverNetworks(adapterRequest);

            int networkIndex = 1;
            Console.WriteLine();
            Console.WriteLine("Discovered Networks");
            foreach (var item in networkList.Networks)
                Console.WriteLine($"  {networkIndex++}  SSID:{item.Ssid} Signal:{item.SignalLevel} SecurityType:{item.SecurityType} ");

            String ssid = ChooseAP(networkList);

            if (ssid != "")
            {
                Console.WriteLine($"Enter password for {ssid}:");
                String password;
                if ((password = Console.ReadLine()) == null)
                    password = "";

                Console.WriteLine();
                Console.WriteLine("Connecting to Network:" + ssid);

                WirelessConnectionRequest request = new()
                {
                    AdapterId = adapterRequest.AdapterId,
                    Ssid = ssid,
                    PassKey = password,
                    Remember = true,
                    Encrypted = false,
                    WaitForIPAddressSeconds = 30 // Don't wait for IP Address
                };


                client.Connect(request);
                connectComplete.WaitOne();

                // Wait for IP Address
                IPInfo info = client.GetIPInfo(adapterRequest);
                Console.WriteLine($"  Address is {info.IPAddress}");

                Console.WriteLine();
                Console.WriteLine("You should now be able to access the Internet from another shell.");
                Console.WriteLine("Hit <enter> to disconnect.");
                Console.WriteLine("Hit <q> to disconnect and end program.");

                key = Console.ReadLine();

                Console.WriteLine();
                Console.WriteLine("Disconnecting Network:" + ssid);
                client.Disconnect(adapterRequest);
                disconnectComplete.WaitOne();
            }

            client.DisableAdapter(adapterRequest);
        }

        Console.WriteLine();
        Console.WriteLine("Network Profiles");
        var profiles = client.GetNetworkProfiles(new ProfileRequest() { AdapterId = adapterRequest.AdapterId });
        foreach (var item in profiles.NetworkProfiles)
            Console.WriteLine($"   SSID:{item.Ssid} SecurityType:{item.PassKey}");

        // Empty the List
        client.SetNetworkProfiles(new NetworkProfilesList() { AdapterId = adapterRequest.AdapterId });

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();
    }

    internal static void TestNetworkServiceAP()
    {
        // Start NetworkService
        Console.WriteLine($"Starting Runtimes");

        // Start client for NetworkService
        var client = new NetworkServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                    .AddClients(client)
                    .StartWithInternalServices();

        Console.WriteLine($"Getting Adapters");
        var networkAdapters = client.GetNetworkAdapters();
        Console.WriteLine($"Found {networkAdapters.Adapters.Count} Adapters");

        // Fetch Client Adapter
        var adapter = networkAdapters.Adapters.First(x => x.AdapterType == NetworkAdapterType.WifiAccessPoint);
        var adapterRequest = new AdapterRequest() { AdapterId = adapter.AdapterId };

        Console.WriteLine();
        Console.WriteLine($"Adapter: {adapter.Description}\r\n is of type {adapter.AdapterType} with AdapterId {adapter.AdapterId}");
        var config = client.GetNetworkConfiguration(adapterRequest);

        Console.WriteLine();
        Console.WriteLine($"Adapter is Configured for {adapter.AdapterType}");

        bool configurationResult = false;
        while (!configurationResult)
        {
            Console.WriteLine($"Enter SSID:");
            String ssid;
            if ((ssid = Console.ReadLine()) == null)
                ssid = "MyDisplay";

            Console.WriteLine($"Enter password for {ssid}:");
            String password;
            if ((password = Console.ReadLine()) == null)
                password = "password";

            Console.WriteLine($"Enter channel for {ssid}:");
            String channel;
            if ((channel = Console.ReadLine()) == null)
                channel = "1";

            int.TryParse(channel, out int channelInt); 

            // Configure
            config.AccessPointConfiguration = new()
            {
                BroadcastSsid = true,
                Ssid = ssid,
                SecurityType = SecurityType.Wpa2,
                EncryptionPassKey = password,
                ChannelNumber = channelInt
            };

            // Configure
            config.DhcpServerConfiguration = new()
            {
                DhcpIpBaseAddress = "192.168.5.1/24",
                Dns = "8.8.8.8",
                DhcpBasePoolSize = 10,
                DhcpBaseOffset = 50,
            };

            // Set Configuration
            Console.WriteLine();
            Console.WriteLine($"Setting Configuration for {config.AdapterId}");
            var result = client.SetNetworkConfiguration(config);

            configurationResult = result.ConfigurationSuccessful;
            if (!configurationResult)
                Console.WriteLine(result.ErrorDescription);
        }

        // Enable Adapter
        Console.WriteLine();
        Console.WriteLine($"Enable Adapter {adapterRequest.AdapterId}");
        client.EnableAdapter(adapterRequest);

        // Wait for Enable Complete
        Console.WriteLine($"Waiting for Adapter To Be Ready");
        enableComplete.WaitOne();

        Console.WriteLine($"Press Enter to End Test");
        Console.ReadLine();

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();
    }

    private static string ChooseAP(WirelessNetworkInfoList apList)
    {
        String chosenSSID = "";

        Console.WriteLine("Please enter the number of the AP to connect: ");
        String choice = Console.ReadLine();
        if (choice != null && Int32.TryParse(choice, out int apNumber))
        {
            if (apNumber > 0 && apNumber <= apList.Networks.Count)
            {
                var ap = apList.Networks[apNumber - 1];
                chosenSSID = ap.Ssid;
            }
        }
        return chosenSSID;
    }

    private static void Client_NotificationReceived(object sender, NotificationEventArgs<NetworkMessageTypes.Ids> e)
    {
        if (e.TransportId == NetworkMessageTypes.Ids.ConnectionStateNotification)
        {
            var response = e.NotificationObject as WirelessConnectionNotification;

            Console.WriteLine("   Connection Status Update " + response.State.ToString());

            if (response.State is WirelessConnectionState.ClientConnected or
                WirelessConnectionState.ClientConnectionFailed)
                connectComplete.Set();

            if (response.State == WirelessConnectionState.ClientDisconnected)
                disconnectComplete.Set();
        }
        else if (e.TransportId == NetworkMessageTypes.Ids.NetworkAdapterReady)
        {
            enableComplete.Set();
        }
    }

}
