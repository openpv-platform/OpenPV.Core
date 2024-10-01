using Ahsoka.ServiceFramework;
using Ahsoka.Services.Video;
using Ahsoka.System;
using Azure.Storage.Blobs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

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
        client.Start();

        // Test Backlight
        BrightnessTest(client);

        // Test USB Role Swapping
        bool runUSBTest = false;
        if (runUSBTest)
        {
            USBTest(client);
        }


        // Test Update Touchscreen
        TestUpdateTouchscreen(client);

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
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

    static AutoResetEvent resetEvent = new AutoResetEvent(false);
    internal static void TestVideo(string videoPath)
    {
        // Start SystemService
        Console.WriteLine($"Starting Runtimes for SystemService Test");

        // Start client for NetworkService
        var client = new VideoServiceClient();
        client.NotificationReceived += (o,e) => 
        {
            if (e.TransportId == VideoMessageTypes.Ids.StatusUpdated)
            {
                VideoStatus status = (VideoStatus)e.NotificationObject;
                Console.WriteLine($"Video Playback {status.VideoPlayerID}  State:{status.IsPlaying} VideoElapsed: {status.PositionInMs / 1000}s of {status.TotalTimeInMs / 1000}s");

                // Video 1 Finished
                if (!status.IsPlaying && status.VideoPlayerID == 1)
                    resetEvent.Set();
            }
        };

        client.Start();

        client.PlayVideo(new VideoPlaybackRequest()
        {
            PlayFullScreen = true,
            StatusUpdateInMs = 1000,
            VideoURL = videoPath,
            VideoPlayerID = 0,
            VideoHeight = 200,
            VideoWidth = 300,
        });

        Thread.Sleep(10000);

        client.PlayVideo(new VideoPlaybackRequest()
        {
            PlayFullScreen = true,
            StatusUpdateInMs = 1000,
            VideoURL = videoPath,
            VideoPlayerID = 1,
            VideoHeight = 200,
            VideoWidth = 300,
        });


        Thread.Sleep(10000);

        client.StopVideo(new VideoPlayer() { VideoPlayerID = 0 });


        Console.WriteLine($"Waiting for End of Video");
        resetEvent.WaitOne();


        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }
}
