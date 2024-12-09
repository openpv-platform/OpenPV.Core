using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.Network;
using Ahsoka.Services.Network.Platform;
using Ahsoka.System;
using Ahsoka.Utility.WiFi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class NetworkServiceTest : LinearTestBase
{
    static readonly bool testEnabled = false;// Disabled for local builds so it doesn't disable your wifi.

    readonly AutoResetEvent connectComplete = new(false);
    readonly AutoResetEvent disconnectComplete = new(false);
    public TestContext TestContext { get; set; }


    [TestMethod]
    public void TestNetworkService()
    {
        // Start client for NetworkService
        var client = new NetworkServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        client.Start();

        string wifiAdapterId = null;

        Console.WriteLine($"Getting Network Adapters");
        var networkAdapters = client.GetNetworkAdapters();
        Console.WriteLine($"Adapters: {networkAdapters.Adapters.Count()}");
        foreach (var adapter in networkAdapters.Adapters)
        {
            Console.WriteLine();
            Console.WriteLine($"Adapter: {adapter.Description}\r\n is of type {adapter.AdapterType} with AdapterId {adapter.AdapterId}");

            var config = client.GetNetworkConfiguration(new AdapterRequest() { AdapterId = adapter.AdapterId });

            // Configure WiFi network if Wireless Adapter
            if (adapter.AdapterType == NetworkAdapterType.WifiClient)
            {
                wifiAdapterId = adapter.AdapterId;

                config.ClientConfiguration = new IPConfiguration()
                {
                    ConnectionType = IPAddressConfiguration.Dhcp
                };
            }

            client.SetNetworkConfiguration(config);
            client.EnableAdapter(new AdapterRequest() { AdapterId = adapter.AdapterId });

            var updatedConfig = client.GetNetworkConfiguration(new AdapterRequest() { AdapterId = adapter.AdapterId });

            Assert.AreEqual(config.AdapterId, updatedConfig.AdapterId);
            Assert.AreEqual(config.ClientConfiguration?.ConnectionType, updatedConfig.ClientConfiguration?.ConnectionType);
        }

        if (wifiAdapterId != null)
        {
            var wifiRequest = new AdapterRequest() { AdapterId = wifiAdapterId };
            var networkList = client.DiscoverNetworks(wifiRequest);

            Console.WriteLine();
            Console.WriteLine("Discovered Networks");

            foreach (var item in networkList.Networks)
                Console.WriteLine($"   SSID:{item.Ssid} Signal:{item.SignalLevel} SecurityType:{item.SecurityType} ");


            Console.WriteLine();
            Console.WriteLine("Network Profiles");

            // Get Profiles Using OS first
            var profiles = client.GetNetworkProfiles(new ProfileRequest() { AdapterId = wifiAdapterId, UseOSProfiles = true });
            foreach (var item in profiles.NetworkProfiles)
            {
                Console.WriteLine($"  SSID:{item.Ssid} ");
            }

            WirelessConnectionRequest rememberedNetwork = null;

            // Call Connect on Any Local Networks that are in the Remember List
            foreach (var item in networkList.Networks)
            {
                var found = profiles.NetworkProfiles.Find(x => x.Ssid == item.Ssid);
                if (found != null && testEnabled)
                {
                    Console.WriteLine();
                    Console.WriteLine("Connecting to Network:" + item.Ssid);

                    // Save This Profile
                    found.Remember = true;

                    client.Connect(found);
                    connectComplete.WaitOne();

                    var ipAddress = client.GetIPInfo(new AdapterRequest() { AdapterId = found.AdapterId });

                    rememberedNetwork = found;

                    Console.WriteLine();
                    Console.WriteLine("Disconnecting Network:" + item.Ssid);
                    disconnectComplete.Reset();
                    client.Disconnect(new AdapterRequest() { AdapterId = found.AdapterId });
                    disconnectComplete.WaitOne();
                    break;
                }
            }

            if (rememberedNetwork != null)
            {
                // Get Saved Networks using Ahsoka Services
                profiles = client.GetNetworkProfiles(new ProfileRequest() { AdapterId = wifiAdapterId, UseOSProfiles = false });
                var profileSaved = profiles.NetworkProfiles.FirstOrDefault(x => x.Ssid == rememberedNetwork.Ssid);
                Assert.IsNotNull(profileSaved);

                // Empty the List
                client.SetNetworkProfiles(new NetworkProfilesList() { AdapterId = wifiAdapterId });

                // Get Again to Validate Save works.
                profiles = client.GetNetworkProfiles(new ProfileRequest() { AdapterId = wifiAdapterId, UseOSProfiles = false });
                Assert.IsTrue(profiles.NetworkProfiles.Count == 0);
            }
        }

        if (wifiAdapterId != null)
            client.DisableAdapter(new AdapterRequest() { AdapterId = wifiAdapterId });

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    [TestMethod]
    public void TestNetworkModel()
    {
        // Start NetworkService
        AutoResetEvent reset = new(false);

        // Start client for NetworkService
        var deviceClient = new DeviceServiceClient(); 
        var client = new NetworkServiceClient();

        // Find Adapter that Supports This Service
        var wifiDevice = deviceClient.GetDevices(new DeviceInfoRequest() { ServiceSearch = "WiFiClient" }).Devices.FirstOrDefault();
        if (wifiDevice != null)
        {
            deviceClient.EnableDevice(wifiDevice);

            var networkAdapters = client.GetNetworkAdapters();
            if (networkAdapters.Adapters.Any(x => x.AdapterType == NetworkAdapterType.WifiClient))
            {
                WiFiNetworkManager manager = new();
                manager.Init(client, deviceClient);

                manager.ConnectionStateChanged += (o, a) =>
                {
                    // Connection State will trigger 
                    // once the init is completed and the firrst discover happens if needed.
                    reset.Set();
                };
                manager.SelectedNetworkChanged += (o, e) => { };
                manager.NetworkListChanged += (o, e) => { };
                manager.AdapterEnabledChanged += (o, e) => { };

                // Wait for Init to Complete
                reset.WaitOne();

                manager.MoveDown();
                manager.MoveUp();
            }

            deviceClient.DisableDevice(wifiDevice);

        }
        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    //[TestMethod]
    public void TestNetworkDClientTools()
    {
        var clientConfig = new AdapterConfiguration() { AdapterId = "ethtest0" };
        NetworkServiceHelper.ReadClientConfiguration(clientConfig);

        // Configure for DHCP
        clientConfig.ClientConfiguration.ConnectionType = IPAddressConfiguration.Dhcp;
        clientConfig.SaveAsDefault = true;

        NetworkServiceHelper.WriteClientConfiguration(clientConfig);
        NetworkServiceHelper.ReadClientConfiguration(clientConfig, true);

        // Verify
        Assert.IsTrue(clientConfig.ClientConfiguration.ConnectionType == IPAddressConfiguration.Dhcp);

        // Configure for StaticIP
        clientConfig.ClientConfiguration.ConnectionType = IPAddressConfiguration.StaticIP;
        clientConfig.ClientConfiguration.IPAddress = "10.0.0.1/24";
        clientConfig.ClientConfiguration.GatewayIP = "10.0.0.1";
        clientConfig.ClientConfiguration.Dns = "1.1.1.1";
        clientConfig.SaveAsDefault = false; // Don't Save Across Reboots

        NetworkServiceHelper.WriteClientConfiguration(clientConfig);
        NetworkServiceHelper.ReadClientConfiguration(clientConfig, false); // Read Current Value

        Assert.IsTrue(clientConfig.ClientConfiguration.ConnectionType == IPAddressConfiguration.StaticIP);
        Assert.IsTrue(clientConfig.ClientConfiguration.IPAddress == "10.0.0.1/24");
        Assert.IsTrue(clientConfig.ClientConfiguration.GatewayIP == "10.0.0.1");
        Assert.IsTrue(clientConfig.ClientConfiguration.Dns == "1.1.1.1");

        NetworkServiceHelper.ReadClientConfiguration(clientConfig, true); // Read Default Again
        Assert.IsTrue(clientConfig.ClientConfiguration.ConnectionType == IPAddressConfiguration.Dhcp);

        Directory.Delete(SystemInfo.HardwareInfo.TargetPathInfo.GetServiceDataPath(System.Hardware.ServiceDataPaths.NetworkProfileFolder), true);
    }

    [TestMethod]
    public void TestPasswordEncryption()
    {
        // start network runtimes

        // Start client for NetworkService
        var client = new NetworkServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        client.Start();

        // Get Adapter
        var networkAdapters = client.GetNetworkAdapters();
        var wifiAdapter = networkAdapters.Adapters.FirstOrDefault(x => x.AdapterType == NetworkAdapterType.WifiClient);
        if (wifiAdapter != null)
        {

            AdapterRequest defaultAdapterRequest = new() { AdapterId = wifiAdapter.AdapterId };
            var testPassword = "testPassword";

            // Create Request
            WirelessConnectionRequest requestNetwork = new()
            {
                AdapterId = defaultAdapterRequest.AdapterId,
                PassKey = testPassword,
                Remember = true,
                Ssid = "TestNetwork",
                Encrypted = false
            };

            // Encrypt and read profiles back
            NetworkProfileHelper.AddNetworkProfile(requestNetwork);
            ProfileRequest requestProfile = new() { AdapterId = defaultAdapterRequest.AdapterId, UseOSProfiles = false };
            NetworkProfilesList networkProfilesList = client.GetNetworkProfiles(requestProfile);

            var profile = networkProfilesList.NetworkProfiles.FirstOrDefault(x => x.Ssid == requestNetwork.Ssid);

            // Decrypt and compare
            var network = NetworkProfileHelper.DecryptNetwork(profile);
            Assert.AreEqual(testPassword, network.PassKey);

            // Empty the List
            client.SetNetworkProfiles(new NetworkProfilesList() { AdapterId = defaultAdapterRequest.AdapterId });

            // Get Again to Validate Save works.
            networkProfilesList = client.GetNetworkProfiles(new ProfileRequest() { AdapterId = defaultAdapterRequest.AdapterId, UseOSProfiles = false });
            Assert.IsTrue(networkProfilesList.NetworkProfiles.Count == 0);
        }

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    [TestMethod]
    public void TestProfileHelper()
    {
        WirelessConnectionRequest requestNetwork = new()
        {
            AdapterId = "Test0",
            PassKey = "Test",
            Remember = true,
            Ssid = "MyNetwork",
            Encrypted = false
        };

        // Encrypt and read profiles back
        NetworkProfileHelper.SetNetworkProfiles(new NetworkProfilesList() { AdapterId = requestNetwork.AdapterId });

        NetworkProfileHelper.AddNetworkProfile(requestNetwork);
        NetworkProfilesList networkProfilesList = NetworkProfileHelper.GetNetworkProfiles(requestNetwork.AdapterId);
        Assert.IsTrue(networkProfilesList.NetworkProfiles.Any());
        NetworkProfileHelper.SetNetworkProfiles(networkProfilesList);

        var profile = networkProfilesList.NetworkProfiles.FirstOrDefault(x => x.Ssid == requestNetwork.Ssid);
        var result = NetworkProfileHelper.DecryptNetwork(profile);
        Assert.IsTrue(result.PassKey == "Test");
    }


    private void Client_NotificationReceived(object sender, NotificationEventArgs<NetworkMessageTypes.Ids> e)
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
    }
}
