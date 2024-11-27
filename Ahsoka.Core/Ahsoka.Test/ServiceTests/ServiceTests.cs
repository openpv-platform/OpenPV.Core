using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services;
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
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Test;

[TestClass]
public class ServiceTests : LinearTestBase
{
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

    readonly Semaphore messageReceived = new(0, 1);

    [TestMethod]
    public void TestNotifications()
    {
        SystemServiceClient systemClient = new();
        systemClient.Start();

        var receiveCount = 0;
        var totalRuns = 100000;

        // List to Message Notifications
        systemClient.NotificationReceived += (sender, args) =>
        {
            if (args.TransportId == SystemMessageTypes.Ids.TestNotif)
            {
                receiveCount++;
                if (receiveCount == totalRuns)
                    messageReceived.Release();
            }
        };

        Stopwatch watch = new();
        watch.Start();
        for (int i = 0; i < totalRuns; i++)
        {
            systemClient.EchoNotif("Test Value"); 
        }

        messageReceived.WaitOne(30000);

        if (receiveCount == totalRuns)
            Assert.AreEqual(totalRuns, receiveCount);

        watch.Stop();

        Console.WriteLine("");
        Console.WriteLine($"Performance Test: {receiveCount} Request/Response Calls completed in {watch.ElapsedMilliseconds}ms");
        Console.WriteLine($"                : {((float)receiveCount / watch.ElapsedMilliseconds) * 1000} Calls Per Second");
        Console.WriteLine("");
    }

    [TestMethod]
    public void TestBaseServiceComponents()
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

        var usb = systemClient.GetUsbInfo();
        systemClient.SetUsbInfo(usb);

        // Request Service Shutdown
        systemClient.RequestServiceShutdown();

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
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
            Thread.Sleep(1000);
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
        NetworkServiceClient netClient = new();

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


