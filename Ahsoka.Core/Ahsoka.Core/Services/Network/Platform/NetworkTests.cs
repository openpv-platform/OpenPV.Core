using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
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
        var client = new WiFiClientServiceClient();
        var deviceClient = new DeviceServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        client.Start();
        deviceClient.Start();

        Console.WriteLine($"Enabling Device");
        // See if we have a WiFiClient
        var wiFiDevice = deviceClient.GetDevices(new DeviceInfoRequest() {  FeatureSearch =   SupportedFeatures.Wifi }).Devices.FirstOrDefault();
        if (wiFiDevice == null)
        {
            Console.WriteLine($"Wifi Device Not Found");
            return;
        }

        // Enable WiFi
        deviceClient.EnableDevice(wiFiDevice);

        var adapterResult = client.GetAdapter();
        var adapter = new AdapterRequest() { AdapterId = adapterResult.AdapterId };
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

}
