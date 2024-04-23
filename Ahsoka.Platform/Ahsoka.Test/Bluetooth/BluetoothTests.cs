using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth;
using Ahsoka.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;

namespace Ahsoka.Test;

[TestClass]
public class BluetoothTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    BluetoothAdapterState setup;

    private static int _deviceCount = 0;

    [TestMethod]
    public void TestBluetoothDiscovery()
    {
        /// Create Client
        var client = StartClient();

        // Get Adapter Configuration
        // This will return a blank adapter setup based on the number of adapters
        // configured in this hardware.
        BluetoothConfiguration configuration = client.GetAdapterConfiguration();

        int adapterIndex = configuration.AdapterConfigurations.First().AdapterIndex;

        // Enable
        client.EnableAdapter(adapterIndex);

        // Set Discoverable and Connectable
        client.SetAdapterState(adapterIndex, true, true);

        // Request Update and check states
        setup = client.RequestAdapterState(adapterIndex);
        if (setup.IsPresent)
        {

            Assert.IsTrue(setup.IsEnabled); // Check Result
            Assert.IsTrue(setup.IsDiscoverable); // Check Result
            Assert.IsTrue(setup.IsConnectable); // Check Result


            // Send Discovery Request - function returns after discovery timeout completion
            client.StartDiscovery(setup.AdapterIndex);

            Debug.WriteLine($"Scan complete. {_deviceCount} device(s) found.");

            // Disable
            client.DisableAdapter(setup.AdapterIndex);

            // Request Update and check enabled state
            setup = client.RequestAdapterState(setup.AdapterIndex);

            Assert.IsFalse(setup.IsEnabled); // Check Result
        }

        // Stop the Runtimes
        AhsokaRuntime.ShutdownAll();
    }

    private BluetoothServiceClient StartClient()
    {
        // Create Client Runtime and Add Services
        BluetoothServiceClient btClient = new(new ServiceConfiguration() 
        { 
            DataChannel = 5004, 
            NotificationChannel = 6004, 
            ServiceName = "BluetoothService", 
            TcpListenAddress = "*",
            TcpConnectionAddress = "localhost" 
        });

        btClient.NotificationReceived += BTClient_NotificationReceived;
        AhsokaRuntime.CreateBuilder()
                     .AddClients(btClient)
                     .StartWithInternalServices();

        return btClient;
    }

    private void BTClient_NotificationReceived(object sender, NotificationEventArgs<BluetoothMessageTypes.Ids> e)
    {
        if (e.TransportId == BluetoothMessageTypes.Ids.DiscoveryDeviceNotification)
        {
            BluetoothDeviceDiscoveryInfo device = e.NotificationObject as BluetoothDeviceDiscoveryInfo;
            _deviceCount++;
            Debug.WriteLine($"Device Found:[{_deviceCount,3}] -  {device.DeviceMacAddress} RSSI:{device.DeviceRssi,4} [{device.DeviceName}]");
        }
    }
}
