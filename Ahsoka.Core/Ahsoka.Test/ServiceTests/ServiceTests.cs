using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services;
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
        AhsokaRuntime.Default.DisconnectAllEndPoints();
    }


    [TestMethod]
    public void TestServiceErrors()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        systemClient.Connect();

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

      
        systemClient.RequestServiceShutdown();
       

        // Stop the Runtimes
        AhsokaRuntime.Default.DisconnectAllEndPoints();
    }

    [TestMethod]
    public void TestBaseServiceComponents()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        systemClient.Connect();

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
        AhsokaRuntime.Default.DisconnectAllEndPoints();
    }

    [TestMethod]
    public void TestAddRemove()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        SystemService systemService = new();

        AhsokaRuntime.Default.ConnectEndPoints(systemService, systemClient);

        string result = systemClient.EchoValue("MyValue");
        Assert.IsTrue(result.StartsWith("MyValue"));

        AhsokaRuntime.Default.DisconnectEndPoints(systemClient, systemService);

        AhsokaRuntime.Default.DisconnectAllEndPoints();
    }

    [TestMethod]
    public void TestLegacyShutdown()
    {
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new();
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        var set = AhsokaRuntime.CreateBuilder()
                     .AddClients(systemClient)
                     .StartWithInternalServices();

        // Request Service Shutdown
        set.RequestShutdown();
        set.WaitForShutdown();

        set = AhsokaRuntime.CreateBuilder()
                    .AddClients(systemClient)
                    .StartWithInternalServices();

        AhsokaRuntime.ShutdownAll();
    }

    [TestMethod]
    public void TestMissingServiceConfig()
    {
        var service1 = ConfigurationLoader.GetServiceConfig("TestConfig");
        var service2 = ConfigurationLoader.GetServiceConfig("TestConfig2");

        Assert.IsFalse(service1.DataListenURI.Equals(service2.DataListenURI));
    }


    [TestMethod]
    public void TestBaseServiceExternalComponents()
    {
        // Test Configuration Builder
        RuntimeConfiguration config = new();
        var systemItem = ConfigurationLoader.GetServiceConfig("SystemService");
        systemItem.Behaviors = EndPointBehaviors.NoBehavior; // Don't AutoStart
        config.Services.Add(systemItem);
       
        string commandLine = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily.Windows64), "CommandLine", "Ahsoka.CommandLine.exe");
      
        // Create Client Runtime and Add Services
        SystemServiceClient systemClient = new(systemItem);
        systemClient.NotificationReceived += ApplicationServiceClient_NotificationReceived;
        systemClient.Connect();

        AhsokaRuntime.StartProcessExecutable(commandLine, [systemClient]);

        
        // Run Sample Test Code
        var task = Task.Run(() =>
        {
            ServiceSelfTests.RunEchoPerfTest(10000, systemClient);
            ServiceSelfTests.PrintServerMetrics(systemClient);

            // Request Service Shutdown
            systemClient.RequestServiceShutdown();
        });

        var result = task.Wait(10000);

        // Stop the Runtimes
        AhsokaRuntime.Default.DisconnectAllEndPoints();

        Assert.IsTrue(result,"The test process did not complete");
    }

    private void ApplicationServiceClient_NotificationReceived(object sender, NotificationEventArgs<SystemMessageTypes.Ids> e)
    {
        if (e.TransportId == SystemMessageTypes.Ids.RequestShutdown)
            TestContext.WriteLine("Received Notification" + e.TransportId.ToString());
    }

    [TestMethod]
    public void TestWindowsPrep()
    {
        var progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

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
        AhsokaRuntime.Default.DisconnectAllEndPoints();
    }
}


