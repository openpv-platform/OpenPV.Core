using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ahsoka.Services.System.Platform;

// Can't be run on Windows
[ExcludeFromCodeCoverage]
internal class SystemTests
{
    public static void TestSystemService()
    {
        // Start SystemService
        Console.WriteLine($"Starting Runtimes for SystemService Test");

        // Start client for NetworkService
        var client = new SystemServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                    .AddClients(client)
                    .StartWithInternalServices();


        // Test Backlight
        BrightnessTest(client);

        // Testing buzzer
        BuzzerTest(client);

        // Test USB Role Swapping
        bool runUSBTest = false;
        if (runUSBTest)
        {
            USBTest(client);
        }

        TestIGNPinAndVBat(client);

        // Test Update Touchscreen
        TestUpdateTouchscreen(client);

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();
    }

    public static void MonitorIgnitionNotification()
    {
        // Start SystemService
        Console.WriteLine($"Starting Runtimes for Ignition Notification Monitor");

        // Start client for NetworkService
        var client = new SystemServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                    .AddClients(client)
                    .StartWithInternalServices();

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();
    }

    private static void USBTest(SystemServiceClient client)
    {
        Thread.Sleep(1000);
        Console.WriteLine("Starting USB Role Swapping Test in SystemService");
        client.GetUsbInfo();
        Thread.Sleep(1000);
        Console.WriteLine("Setting USB-C port to Host mode for 20 seconds. Check for USB Stick");
        UsbInfo usbInfo = new()
        {
            Mode = UsbMode.Host,
            MountPoint = "/mnt/stick"
        };
        client.SetUsbInfo(usbInfo);
        Thread.Sleep(100);
        client.GetUsbInfo();
        Thread.Sleep(20000);
        Console.WriteLine("Setting USB-C port to Device mode. Check for Gadget connection");
        usbInfo.Mode = UsbMode.Device;
        usbInfo.MountPoint = "";
        client.SetUsbInfo(usbInfo);
        Thread.Sleep(100);
        client.GetUsbInfo();
        Console.WriteLine("Completed USB Role Swapping Test in SystemService");
    }

    private static void BuzzerTest(SystemServiceClient client)
    {
        Console.WriteLine("Starting Buzzer Test");
        client.GetBuzzerConfig();
        BuzzerConfig buzzerConfig = new()
        {
            IsEnabled = true,
            FrequencyInHz = 2000,
            VolumePct = 25
        };
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.FrequencyInHz = 1500;
        buzzerConfig.VolumePct = 50;
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.FrequencyInHz = 1000;
        buzzerConfig.VolumePct = 75;
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.FrequencyInHz = 500;
        buzzerConfig.VolumePct = 100;
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.IsEnabled = false;
        client.SetBuzzerConfig(buzzerConfig); // buzzer is switched off
        Console.WriteLine("Completed Buzzer Test");
    }

    private static void BrightnessTest(SystemServiceClient client)
    {
        Console.WriteLine("Starting Backlight Test in SystemService");
        client.GetBrightness();
        Thread.Sleep(1000);
        BrightnessInfo brightnessInfo = new();
        for (int i = 0; i <= 100; i++)
        {
            brightnessInfo.Percentage = i;
            BrightnessInfo response = client.SetBrightness(brightnessInfo);
            Console.WriteLine($"Set brightness to {brightnessInfo.Percentage}%, response {response.Percentage}%");
            client.GetBrightness();
            Thread.Sleep(70);
        }
        Console.WriteLine("Completed Backlight Test in SystemService");
    }

    private static void Client_NotificationReceived(object sender, NotificationEventArgs<SystemMessageTypes.Ids> e)
    {
        if (e.TransportId == SystemMessageTypes.Ids.GetBrightness)
        {
            var response = e.NotificationObject as BrightnessInfo;
            Console.WriteLine($"Current Brightness is: {response.Percentage}");
        }
        else if (e.TransportId == SystemMessageTypes.Ids.GetUsbInfo)
        {
            var response = e.NotificationObject as UsbInfo;
            Console.WriteLine($"Current USB Mode is: {response.Mode}");
        }
        else if (e.TransportId == SystemMessageTypes.Ids.IgnitionOffNotification)
        {
            Console.WriteLine($"Client: ignition is off");
        }
        else if (e.TransportId == SystemMessageTypes.Ids.IgnitionOnNotification)
        {
            Console.WriteLine($"Client: ignition is on");
        }
    }

    private static void TestIGNPinAndVBat(SystemServiceClient client)
    {
        Console.WriteLine("[] Testing GetVBat and GetIGNPin function calls...");
        VoltageValue vbat = client.GetVBat();
        Console.WriteLine($"[+] VBAT (milli volts) -> {vbat.MilliVolts}");
        IgnitionState ign = client.GetIGNPin();
        Console.WriteLine($"[+] IGN Pin (milli volts) -> {ign.MilliVolts}");
        Console.WriteLine("[] DONE Testing GetVBat and GetIGNPin function calls!");
    }

    private static void TestUpdateTouchscreen(SystemServiceClient client)
    {
        Console.WriteLine("Testing UpdateTouchscreen function calls...");

        {
            var reply = client.UpdateTouchTune("/home/root/maxtouch.cfg", "/home/root/maxtouch.fw");
            Console.WriteLine($"Status = {reply.ReplyStatus}");
            Console.WriteLine($"Reason = {reply.ReplyReason}");
            Console.WriteLine("Tested UpdateTouchscreen configuration and firmware update...\n");
            Thread.Sleep(1000);
        }

        {
            var reply = client.UpdateTouchTune("/home/root/maxtouch.cfg");
            Console.WriteLine($"Status = {reply.ReplyStatus}");
            Console.WriteLine($"Reason = {reply.ReplyReason}");
            Console.WriteLine("Tested UpdateTouchscreen configuration update...\n");
            Thread.Sleep(1000);
        }

        {
            var reply = client.UpdateTouchTune();
            Console.WriteLine($"Status = {reply.ReplyStatus}");
            Console.WriteLine($"Reason = {reply.ReplyReason}");
            Console.WriteLine("Tested UpdateTouchscreen update, id only...\n");
            Thread.Sleep(1000);
        }

        Console.WriteLine("Completed UpdateTouchscreen in SystemService");
    }
}
