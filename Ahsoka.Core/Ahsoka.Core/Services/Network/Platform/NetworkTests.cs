using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
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
        var client = new WiFiClientServiceClient();
        var deviceClient = new DeviceServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        client.Start();
        deviceClient.Start();

        Console.WriteLine($"Enabling Device");
        // See if we have a WiFiClient
        var wiFiDevice = deviceClient.GetDevices(new DeviceInfoRequest() { FeatureSearch = SupportedFeatures.Wifi }).Devices.FirstOrDefault();
        if (wiFiDevice == null)
        {
            Console.WriteLine($"Wifi Device Not Found");
            return;
        }

        // Enable WiFi
        deviceClient.EnableDevice(wiFiDevice);

        var adapterResult = client.GetAdapters();
        var adapter = adapterResult.Adapters.FirstOrDefault();
        var config = client.GetNetworkConfiguration(adapter);

        // Configure
        if (config.ClientConfiguration.ConnectionType != IPAddressConfiguration.Dhcp)
        {
            config.ClientConfiguration = new IPConfiguration() { ConnectionType = IPAddressConfiguration.Dhcp };
            config.SaveAsDefault = true;
            Console.WriteLine();
            Console.WriteLine($"Setting Configuration for {config.AdapterId} to DHCP");
            client.SetNetworkConfiguration(config);
        }

        string key = "";

        while (key != "q")
        {
            Console.WriteLine();
            Console.WriteLine($"List Networks in Range");
            var networkList = client.DiscoverNetworks(adapter);

            int networkIndex = 1;
            Console.WriteLine();
            Console.WriteLine("Discovered Networks");
            foreach (var item in networkList.Networks)
                Console.WriteLine($"  {networkIndex++}  SSID:{item.Ssid} Signal:{item.SignalLevel} Status:{item.State}  SecurityType:{item.SecurityType} ");

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
                    AdapterId = adapter.AdapterId,
                    Ssid = ssid,
                    PassKey = password,
                    Remember = true,
                    Encrypted = false,
                    WaitForIPAddressSeconds = 30 // Don't wait for IP Address
                };


                client.Connect(request);
                connectComplete.WaitOne();

                // Wait for IP Address
                IPInfo info = client.GetIPInfo(adapter);
                Console.WriteLine($"  Address is {info.IPAddress}");

                Console.WriteLine();
                Console.WriteLine("You should now be able to access the Internet from another shell.");
                Console.WriteLine("Hit <enter> to disconnect.");
                Console.WriteLine("Hit <q> to disconnect and end program.");

                key = Console.ReadLine();

                Console.WriteLine();
                Console.WriteLine("Disconnecting Network:" + ssid);
                client.Disconnect(adapter);
                disconnectComplete.WaitOne();
            }
        }

        Console.WriteLine();
        Console.WriteLine("Network Profiles");
        var profiles = client.GetNetworkProfiles(new ProfileRequest() { AdapterId = adapter.AdapterId });
        foreach (var item in profiles.NetworkProfiles)
            Console.WriteLine($"   SSID:{item.Ssid} SecurityType:{item.PassKey}");

        // Empty the List
        client.SetNetworkProfiles(new NetworkProfilesList() { AdapterId = adapter.AdapterId });

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
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

    private static void Client_NotificationReceived(object sender, NotificationEventArgs<WiFiClientMessageTypes.Ids> e)
    {
        if (e.TransportId == WiFiClientMessageTypes.Ids.ConnectionStateNotification)
        {
            var response = e.NotificationObject as WirelessConnectionNotification;

            Console.WriteLine("   Connection Status Update " + response.State.ToString());

            if (response.State is WirelessConnectionState.ClientConnected or
                WirelessConnectionState.ClientConnectionFailed)
                connectComplete.Set();

            if (response.State == WirelessConnectionState.ClientDisconnected)
                disconnectComplete.Set();
        }
    }


    internal static void TestNetworkServiceAP()
    {
        // Start NetworkService
        Console.WriteLine($"Starting Runtimes");

        // Start client for NetworkService
        var client = new WiFiAPServiceClient();
        client.NotificationReceived += AP_NotificationReceived;
        client.Start();

        Console.WriteLine($"Getting Adapter");
        var adapter = client.GetAdapters().Adapters.FirstOrDefault();
        Console.WriteLine($"Found Adapter {adapter.AdapterId}");

        // Fetch Client Adapter
        var adapterRequest = new AdapterRequest() { AdapterId = adapter.AdapterId };

        var config = client.GetNetworkConfiguration(adapterRequest);

        bool configurationResult = false;
        while (!configurationResult)
        {
            Console.WriteLine($"Enter SSID:");
            String ssid = Console.ReadLine();
            if (String.IsNullOrEmpty(ssid))
                ssid = "MyDisplay";
            Console.WriteLine($"Selected:" + ssid);

            Console.WriteLine($"Enter password for {ssid}:");
            String password = Console.ReadLine();
            if (String.IsNullOrEmpty(password))
                password = "password";
            Console.WriteLine($"Selected:" + password);

            Console.WriteLine($"Enter channel for {ssid}:");
            String channel = Console.ReadLine();
            if (String.IsNullOrEmpty(channel))
                channel = "1";
            Console.WriteLine($"Selected:" + channel);

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
        client.StartAccessPoint(adapterRequest);

        Console.WriteLine($"Press Enter to End Test");
        Console.ReadLine();

        client.StopAccessPoint(adapterRequest);
        Console.WriteLine($"Shutdown Complete.");

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }


    private static void AP_NotificationReceived(object sender, NotificationEventArgs<WiFiAPMessageTypes.Ids> e)
    {
        if (e.TransportId == WiFiAPMessageTypes.Ids.ClientAdded)
        {
            var response = e.NotificationObject as AccessPointClient;
            Console.WriteLine($"   Client Connected MAC:{response.MacAddress} Signal: {response.Rssi}");
        }
        if (e.TransportId == WiFiAPMessageTypes.Ids.ClientRemoved)
        {
            var response = e.NotificationObject as AccessPointClient;
            Console.WriteLine($"   Client Disconnected MAC:{response.MacAddress}");
        }
    }


}
