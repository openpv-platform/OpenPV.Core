using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;
using System.ComponentModel;
using System.Linq;

namespace Ahsoka.CS.Devices;

public class Program
{
    static CellularServiceClient cellService = new();
    static MainUI mainUi = new MainUI();

    static SystemServiceClient systemServiceClient = new();
    static DeviceServiceClient deviceServiceClient = new();

    static UsbInfo currentUsbState = new UsbInfo();
    static string cellDeviceStatus = "Not Connected";

    public static void Main()
    {
        Console.WriteLine("Starting Ahsoka Devices Demo Application");

        deviceServiceClient.NotificationReceived += DeviceServiceClient_NotificationReceived;

        // Start All Services
        AhsokaRuntime.Default.StartEndPoints(systemServiceClient, deviceServiceClient, cellService);

        // Note that this app is configured to role swap to "Host" in the configuration
        // but you can do it here if you change the configuration back to Device or Default
        // Host on Primary (Secondary on Pro) is required for handling external devices 
        //deviceServiceClient.SetUsbInfo(new UsbInfo() { Mode = UsbMode.Host, Device = UsbPort.Primary });

        // Start our UI and Setup the Various Buttons and Text Areas
        var window = mainUi.StartUI("OpenPV Device Demo");
        mainUi.AddButton(RollSwap, "Swap USB Roll");

        // These Events will listen for our Model Changes and Refresh Our UI.
        // we are also calling the RefreshUI once to start to init the status values
        RefreshUI(deviceServiceClient, new PropertyChangedEventArgs(""));

        // Startup Ready
        ApplicationContext.ReleaseStartup();

        // Start Device Notifications.  Any Ready Devices will send OnReady (in case ready before now), USB Devices will begin notifying on insert.
        deviceServiceClient.EnableDeviceNotifications();

        // Fitch Initial State of USB
        currentUsbState = deviceServiceClient.GetUsbInfo();

        // Run Main UI
        window.ShowAndRun();


        Console.WriteLine($"Stopping Network");
        var modem = cellService.GetAdapters().Adapters.First();
        cellService.StopNetwork(modem);

        // Use Exit to Shutdown All Services.
        ApplicationContext.Exit();
    }

    private static void RollSwap()
    {
        // Fitch Initial State of USB
        deviceServiceClient.SetUsbInfo(new UsbInfo() { Mode = currentUsbState.Mode == UsbMode.Device ? UsbMode.Host : UsbMode.Device, Port = UsbPort.Primary });
        currentUsbState = deviceServiceClient.GetUsbInfo();

        RefreshUI(deviceServiceClient, new PropertyChangedEventArgs(""));

    }

    private static void RefreshUI(object sender, PropertyChangedEventArgs e)
    {
        mainUi.UpdateStatusText("USB Device State:", currentUsbState.Mode.ToString());
        mainUi.UpdateStatusText("Cellular Device State:", cellDeviceStatus);
    }

    private static void DeviceServiceClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.OnboardDeviceReady) // Only used on Windows since project auto-enables devices
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            DeviceInfo info = e.NotificationObject as DeviceInfo;

            if (info.DeviceType == DeviceTypes.OnboardWifiModule || info.DeviceType == DeviceTypes.OnboardWirelessComboModule)
            {
                mainUi.InitStateChanged(true, "WiFi Ready - Waiting for Devices");
            }
        }

        // Since we are using Auto Enable, we can simply wait for Enabled
        if (e.TransportId == DeviceMessageTypes.Ids.DeviceEnabled)
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            DeviceInfo info = e.NotificationObject as DeviceInfo;

            if (info.DeviceType == DeviceTypes.OnboardWifiModule || info.DeviceType == DeviceTypes.OnboardWirelessComboModule)
            {
                mainUi.InitStateChanged(true, "WiFi Ready - Waiting for Devices");
            }

            if (info.DeviceType == DeviceTypes.UsbCellularNetwork)
            {
                // Enable Cellular Modem
                mainUi.UpdateStatusString("Starting Cell Network");
                Console.WriteLine($"Starting Cell Network");
                StartCellNetwork();
            }
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceAdded)
        {
            // When not using Auto Enable, this is where you could enable the USB Device
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;

            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularNetwork)
            {
                // Enable Cellular Modem
                mainUi.UpdateStatusString($"NOT Enabling Cell Modem Device (Using auto Enable)");
                Console.WriteLine($"NOT Enabling Cell Modem Device (Using auto Enable)");
                // deviceServiceClient.EnableDevice(info.DeviceInfo); // Start client for NetworkService
            }
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceRemoved)
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;

            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularNetwork)
            {
                // Enable Cellular Modem
                mainUi.UpdateStatusString($"Modem Removed");
                Console.WriteLine($"Modem Removed");

                var adapterInfo = cellService.GetAdapters().Adapters.FirstOrDefault();
                cellService.StopNetwork(adapterInfo);
                cellService.NotificationReceived -= CellService_NotificationReceived;

            }
        }

        RefreshUI(deviceServiceClient, new PropertyChangedEventArgs(""));
    }

    public static void StartCellNetwork()
    {
        mainUi.UpdateStatusString($"Starting Cell Service");
        Console.WriteLine($"Starting Cell Service");
        cellService.NotificationReceived += CellService_NotificationReceived;

        // Find Adapter that Supports This Service
        var adapterInfo = cellService.GetAdapters().Adapters.FirstOrDefault();
        if (adapterInfo != null)
            cellService.StartNetwork(new() { AdapterId = adapterInfo.AdapterId, Apn = "hologram" });

    }

    private static void CellService_NotificationReceived(object sender, AhsokaClientBase<CellularMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == CellularMessageTypes.Ids.NetworkStateChanged)
        {
            CellularNetworkInformation info = e.NotificationObject as CellularNetworkInformation;
            Console.WriteLine($"Network Service Status: {info.ConnectionState}");
            Console.WriteLine($"    IPAddress: {info.IpInfo?.IPAddress}");
            Console.WriteLine($"    Service: {info.ServiceName}");
            Console.WriteLine($"    Network: {info.NetworkOperatorName}");
            Console.WriteLine($"    RSSI: {info.Rssi} SNR: {info.Snr}");

            cellDeviceStatus = $"{info.ConnectionState} @ {info.IpInfo?.IPAddress}";

            RefreshUI(deviceServiceClient, new PropertyChangedEventArgs(""));
        }
    }
}
