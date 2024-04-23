using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ahsoka.Test;

[TestClass]
public class HardwareInfoTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void LicenseModelTest()
    {
        foreach (var licenseInfo in SystemInfo.LicenseInfos)
        {
            Console.WriteLine($"{licenseInfo.PackageName}:{licenseInfo.LicenseType}:{licenseInfo.Copyright}:{licenseInfo.PackageVersion}:{licenseInfo.LicenseUrl}");
        }
    }

    [TestMethod]
    public void HardwareInfoTest()
    {
        foreach (var hardwareDescription in HardwareInfo.GetHardwareInfoDescriptions())
        {
            var info = HardwareInfo.GetHardwareInfoByDescription(hardwareDescription);
            Assert.IsNotNull(info);
        }
    }

    [TestMethod]
    public void RuntimeConfig()
    {
        var runtimeconfig = new RuntimeConfiguration();
        runtimeconfig.Services.Add(new ServiceConfiguration()
        {
            DataChannel = 5100,
            NotificationChannel = 6100,
            TcpListenAddress = "127.0.0.1",
            TcpConnectionAddress = "127.0.0.1",
            ServiceName = "FakeConfig",
            SocketType = TransportSocketTypes.SerialSocket,
            Behaviors =  EndPointBehaviors.AutoStart
        });

        string result = JsonUtility.Serialize(runtimeconfig);
        var decoded = JsonUtility.Deserialize<RuntimeConfiguration>(result);

        ConfigurationLoader.SetRuntimeConfiguration(runtimeconfig);

        Assert.IsTrue(runtimeconfig.Services.Any(x => x.ServiceName == "FakeConfig"));

        ConfigurationLoader.SetRuntimeConfiguration(ConfigurationLoader.CreateDefaultConfiguration());

    }
}
