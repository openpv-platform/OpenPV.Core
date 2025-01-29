
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.Gateway;
using Ahsoka.Services.System;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahsoka.Test;

[TestClass]
public class GatewayTests : LinearTestBase
{

    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestGatewayServices()
    {

        // Create Client Runtime and Add Services
        SystemService systemService = new();
        DeviceService deviceService = new();

        systemService.Start();
        deviceService.Start();

        // Example Gateway Service (Could be any Service)
        GatewayService gatewayService = new();
        gatewayService.Start();

        // Example that Impersonates the SystemService - Gateway Service will route to the correct service via the Gateway Socket
        GatewayServiceClient client = new("SystemService");
        client.Start();

        var result = client.SendMessageWithResponse<GetHardwareInformationReply>(SystemMessageTypes.Ids.GetHardwareInformation.EnumToInt(), null);
        Assert.IsTrue(result != null);

        // Example that Impersonates the Device - Gateway Service will route to the correct service via the Gateway Socket
        GatewayServiceClient client2 = new("DeviceService");
        client2.Start();
        Assert.IsTrue(result != null);

        var deviceResult = client2.SendMessageWithResponse<DeviceList>(DeviceMessageTypes.Ids.GetDevices.EnumToInt(), null);

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

}