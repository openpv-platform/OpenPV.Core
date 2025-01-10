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

        // Role Swap...TEMP CODE UNTIL DEFAULTS ARE CREATED
        deviceServiceClient.SetUsbInfo(new UsbInfo() { Mode = UsbMode.Host });
       
        // Startup Ready
        ApplicationContext.ReleaseStartup();

        // Start Device Notifications.  Any Ready Devices will send OnReady (in case ready before now), USB Devices will begin notifying on insert.
        deviceServiceClient.EnableDeviceNotifications();
        
        ApplicationContext.WaitForExitSignal();

        Console.WriteLine($"Stopping Network");
        var modem = cellService.GetAdapters().Adapters.First();
        cellService.StopNetwork(modem);

        ApplicationContext.Exit();
    }

    private static void DeviceServiceClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceAdded)
        {
            DeviceServiceClient deviceServiceClient = sender as DeviceServiceClient;
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;
            
            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularNetwork)
            { 
                // Enable Cellular Modem
                Console.WriteLine($"Enabling Cell Modem Device if Disabled");
                deviceServiceClient.EnableDevice(info.DeviceInfo); // Start client for NetworkService

                StartCellNetwork();
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
