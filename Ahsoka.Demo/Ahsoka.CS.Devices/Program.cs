using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;
using System.Linq;

namespace Ahsoka.Services.Demo;

public class Program
{
    static CellularServiceClient cellService = new();

    public static void Main()
    {
        Console.WriteLine("Starting Ahsoka Devices Demo Application");

        // Start a System Service for talking to OpenPV Desktop if needed.
        SystemServiceClient systemServiceClient = new();
        
        // Start a Device Client (and Service) and listen for Device Ready / Added
        DeviceServiceClient deviceServiceClient = new();
        
        deviceServiceClient.NotificationReceived += DeviceServiceClient_NotificationReceived;

        // Start All Services
        AhsokaRuntime.Default.StartEndPoints(systemServiceClient, deviceServiceClient, cellService);


        // Note that this app is configured to role swap to "Host" in the configuration
        // but you can do it here if you change the configuration back to Device or Default
        // Host on Primary (Secondary on Pro) is required for handling external devices 
        //deviceServiceClient.SetUsbInfo(new UsbInfo() { Mode = UsbMode.Host });
       
        // Startup Ready
        ApplicationContext.ReleaseStartup();

        // Start Device Notifications.  Any Ready Devices will send OnReady (in case ready before now), USB Devices will begin notifying on insert.
        deviceServiceClient.EnableDeviceNotifications();
        
        ApplicationContext.WaitForExitSignal();

        Console.WriteLine($"Stopping Network");
        var modem = cellService.GetAdapters().Adapters.First();
        cellService.StopNetwork(modem);

        // Use Exit to Shutdown All Services.
        ApplicationContext.Exit();
    }

    private static void DeviceServiceClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.DeviceEnabled)
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            DeviceInfo info = e.NotificationObject as DeviceInfo;

            if (info.DeviceType == DeviceTypes.UsbCellularNetwork)
            {
                // Enable Cellular Modem
                Console.WriteLine($"Starting Cell Network");
                StartCellNetwork();
            }
        }

        if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceAdded)
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;
            
            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularNetwork)
            { 
                // Enable Cellular Modem
                Console.WriteLine($"NOT Enabling Cell Modem Device (Using auto Enable)");
               // deviceServiceClient.EnableDevice(info.DeviceInfo); // Start client for NetworkService

                //StartCellNetwork();
            }
        }
        if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceRemoved)
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;

            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularNetwork)
            {
                // Enable Cellular Modem
                Console.WriteLine($"Modem Removed");

                var adapterInfo = cellService.GetAdapters().Adapters.FirstOrDefault();
                cellService.StopNetwork(adapterInfo);
                cellService.NotificationReceived -= CellService_NotificationReceived;

            }
        }
    }

    public static void StartCellNetwork()
    {
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
        }
    }
}
