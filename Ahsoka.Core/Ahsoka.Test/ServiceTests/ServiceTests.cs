using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services;
using Ahsoka.Services.Device;
using Ahsoka.Services.Iot;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.Services.System.Platform;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Test;

[TestClass]
public class ServiceTests : LinearTestBase
{
    static AutoResetEvent enabledEvent = new(false);
    static AutoResetEvent disabledEvent = new(false);


    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestProtoPerformance()
    {
        int count = 10000;

        // Run Perf Test.
        Stopwatch watch = new();
        watch.Start();

        for (int i = 0; i < count; i++)
        {
            byte[] dataHeader = null;
            byte[] dataBody = null;

            AhsokaMessageHeader request = new();
            EchoValue echoRequest = new() { DataValue = "Hello World" };
            using (MemoryStream stream = new())
            {
                ProtoBuf.Serializer.Serialize(stream, request);
                dataHeader = stream.ToArray();
            }

            using (MemoryStream stream = new())
            {
                ProtoBuf.Serializer.Serialize(stream, echoRequest);
                dataBody = stream.ToArray();
            }

            using (MemoryStream stream = new(dataHeader))
            {
                var result = ProtoBuf.Serializer.Deserialize(stream, request);
            }

            using (MemoryStream stream = new(dataBody))
            {
                var result = ProtoBuf.Serializer.Deserialize(stream, echoRequest);
            }
        }
        watch.Stop();

        Console.WriteLine("");
        Console.WriteLine($"Performance Test: {count} Calls completed in {watch.ElapsedMilliseconds}ms");
        Console.WriteLine($"                : {((float)count / watch.ElapsedMilliseconds) * 1000} Calls Per Second");
        Console.WriteLine("");
    }

    [TestMethod]
    public void AutoStartTests()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();

        ServiceSelfTests.PrintServerMetrics(systemClient);

        // Request Service Shutdown
        systemClient.RequestServiceShutdown();

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }


    [TestMethod]
    public void TestServiceErrors()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        systemClient.Start();

        string image = Path.GetFullPath("Ahsoka/Resources/EnovLogo.png");

        // Request Service Shutdown
        try
        {
            // Forced Error
            systemClient.SetBootLogo(new BootLogoInfo()
            {
                Path = "/test/test/test"
            });

            Assert.Fail("Service Exception was not thrown as expected");
        }
        catch (Exception ex)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.Message);
        }

        if (File.Exists(image))
        {
            var bootLogo = new BootLogoInfo()
            {
                Path = image
            };

            // Forced Error
            systemClient.SetBootLogo(bootLogo);

            bootLogo.Rotation = BootLogoRotation.Rotate90;
            systemClient.SetBootLogo(bootLogo);
            
            bootLogo.Rotation = BootLogoRotation.Rotate180;
            systemClient.SetBootLogo(bootLogo);

            bootLogo.Rotation = BootLogoRotation.Rotate270;
            systemClient.SetBootLogo(bootLogo);
        }

        systemClient.RequestServiceShutdown();
       

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    
    [TestMethod]
    public void TestNotifications()
    {
        int receiveConcurrentCount = 0;
        int receiveCount = 0;

        // Create and Start Service
        SystemServiceClient systemClient = new();
        systemClient.Start();
        
        Stopwatch watch = new();
        watch.Start();
        
        // Start a Thread and Run Test
        var task1 = Task.Run(() =>
        {
            RunTest(systemClient, out receiveCount);
        });

        // Create Confurrent Client and Run Test
        // This should use the local service / with a local socket.
        var task2 = Task.Run(() =>
        {
            SystemServiceClient secondClient = new();
            secondClient.Start();

            RunTest(secondClient, out receiveConcurrentCount);

            secondClient.Stop();
        });

        // Wait for Concurrent Task to Finish
        Task.WaitAll(task1, task2);

        watch.Stop();

        Console.WriteLine("");
        Console.WriteLine($"Performance Test: {(receiveCount + receiveConcurrentCount)} Request/Response Calls completed in {watch.ElapsedMilliseconds}ms");
        Console.WriteLine($"                : {((float)(receiveCount + receiveConcurrentCount) / watch.ElapsedMilliseconds) * 1000} Calls Per Second");
        Console.WriteLine("");

        systemClient.Stop();

        AhsokaRuntime.Default.StopAllEndPoints();
    }

    private void RunTest(SystemServiceClient systemClient, out int receiveCount)
    {
        Semaphore messageReceived = new(0, 1);
        
        int totalRuns;

        int receiveCountLocal = 0;
        totalRuns = 100000;

        // List to Message Notifications
        systemClient.NotificationReceived += (sender, args) =>
        {
            if (args.TransportId == SystemMessageTypes.Ids.TestNotif)
            {
                receiveCountLocal++;
                if (receiveCountLocal == totalRuns)
                    messageReceived.Release();
            }
        };

        for (int i = 0; i < totalRuns; i++)
        {
            systemClient.EchoNotif("Test Value");
        }

        messageReceived.WaitOne(30000);

        Assert.AreEqual(totalRuns, receiveCountLocal);
        
        receiveCount = receiveCountLocal;

    }

    [TestMethod]
    public void TestSystemService()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        systemClient.Start();

        // Run Sample Test Code
        ServiceSelfTests.RunEchoPerfTest(100000, systemClient);
        ServiceSelfTests.PrintServerMetrics(systemClient);
      
        var brightness = systemClient.GetBrightness();
        systemClient.SetBrightness(brightness);

        // Request Service Shutdown
        systemClient.RequestServiceShutdown();

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }


    [TestMethod]
    public void TestDeviceService()
    {
        // Create Client Runtime and Add Services
        DeviceServiceClient deviceClient = new();
        deviceClient.NotificationReceived += SystemClient_NotificationReceived;
        deviceClient.Start();

        var usbInfo = deviceClient.GetUsbInfo();
        deviceClient.SetUsbInfo(usbInfo);


        var wifiDevice = deviceClient.GetDevices(new DeviceInfoRequest() { FeatureSearch =  SupportedFeatures.Wifi }).Devices.FirstOrDefault();
        deviceClient.EnableDevice(wifiDevice);
        Assert.IsTrue(enabledEvent.WaitOne(2000));
        deviceClient.DisableDevice(wifiDevice);
        Assert.IsTrue(disabledEvent.WaitOne(2000));

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    private void SystemClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.DeviceEnabled)
        {
            enabledEvent.Set();
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.DeviceDisabled)
        {
            disabledEvent.Set();
        }
    }

    [TestMethod]
    public void TestAddRemove()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        SystemService systemService = new();

        AhsokaRuntime.Default.StartEndPoints(systemService, systemClient);

        string result = systemClient.EchoValue("MyValue");
        Assert.IsTrue(result.StartsWith("MyValue"));

        AhsokaRuntime.Default.StopEndPoints(systemClient, systemService);

        AhsokaRuntime.Default.StopAllEndPoints();
    }

    [TestMethod]
    public void TestLegacyShutdown()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        systemClient.Start();

        // Request Service Shutdown
        systemClient.Stop();
        systemClient.WaitForStop();

        systemClient.Start();

        AhsokaRuntime.Default.StopAllEndPoints();
    }

    [TestMethod]
    public void TestMissingServiceConfig()
    {
        var service1 = ConfigurationLoader.GetServiceConfig("TestConfig");
        var service2 = ConfigurationLoader.GetServiceConfig("TestConfig2");

        Assert.IsFalse(service1.DataListenURI.Equals(service2.DataListenURI));
    }


    private void ApplicationServiceClient_NotificationReceived(object sender, NotificationEventArgs<SystemMessageTypes.Ids> e)
    {
        if (e.TransportId == SystemMessageTypes.Ids.RequestShutdown)
            TestContext.WriteLine("Received Notification" + e.TransportId.ToString());
    }

    [TestMethod]
    public void TestWindowsPrep()
    {
        var progress = new Progress<string>(Console.WriteLine);

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(PlatformFamily.Windows64);

        // Create Hardware Info
        HardwareInfo hardware = HardwareInfo.GetHardwareInfo(PlatformFamily.Windows64, "Desktop");

        var info = new TargetConnectionInfo() { PlatformFamily = hardware.PlatformFamily, PlatformQualifier = hardware.PlatformQualifier, HostName = "localhost", UserName = "" };

        bool returnValue = prep.Prep(info, null, progress);
    }

    [TestMethod]
    public void TestCommandLineItems()
    {
        CommandLine.Execute(new string[] { "--GetHardwareModel" });

        CommandLine.Execute(new string[] { "--GetHardwareSerialNumber" });

        CommandLine.Execute(new string[] { "--Verify" });
    }

    [TestMethod]
    public void TestAppContext()
    {
        Task.Run(() =>
        {
            Thread.Sleep(10);
            ApplicationContext.Exit();
        });

        // Wait for Exit to Signal
        ApplicationContext.WaitForExitSignal();
    }

    [TestMethod]
    public void TestDispatcherComponents()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        WiFiClientServiceClient netClient = new();

        Dispatcher.Default.RegisterCallback((o, e) => { }, netClient);
        Dispatcher.Default.RegisterCallback<NotificationEventArgs<SystemMessageTypes.Ids>>(ApplicationServiceClient_NotificationReceived, systemClient);
        Dispatcher.Default.RegisterTimerCallback((o, e) =>
        {
            Console.WriteLine("Do Nothing");

        }, TimeSpan.FromSeconds(1), systemClient);

        Dispatcher.Default.RegisterTimerCallback((o, e) =>
        {
            Dispatcher.Default.Stop();

        }, TimeSpan.FromSeconds(1), systemClient);

        Dispatcher.Default.InvokeDispatcher(() =>
        {
            ServiceSelfTests.PrintServerMetrics(systemClient);
        });

        Dispatcher.Default.InvokeDispatcherAsync(() =>
        {
            return true;
        },
        () =>
        {
            // Do Nothing
        });

        Dispatcher.Default.InvokeDispatcherAsync<object>(() =>
        {
            return new object();
        },
        (o) =>
        {
            // Do Nothing
        });

        Dispatcher.Default.StartAndRun();

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }
}


