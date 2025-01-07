using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;

namespace Ahsoka.Services.Demo;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Starting Ahsoka Devices Demo Application");

        // Create a Service Client for accessign system features.
        // this is useful as it allows the OpenVPV Customer Toolkit to talk 
        // to our application.  If this behavior is not wanted, the clients configuration
        // can be altered to only listen to internal url's OR not started at all.
        SystemServiceClient systemServiceClient = new();
        DeviceServiceClient deviceServiceClient = new();

        // Role Swap
        deviceServiceClient.SetUsbInfo(new UsbInfo() { Mode = UsbMode.Host });

        deviceServiceClient.NotificationReceived += DeviceServiceClient_NotificationReceived;


        AhsokaRuntime.Default.StartEndPoints(systemServiceClient, deviceServiceClient);
       
        ApplicationContext.WaitForExitSignal();
        ApplicationContext.Exit();
    }

    private static void DeviceServiceClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceAdded)
        {
            Console.WriteLine("Device Added");
        }
    }
}
