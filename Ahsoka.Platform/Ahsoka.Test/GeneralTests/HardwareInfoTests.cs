using Ahsoka.System;
using Ahsoka.System.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ahsoka.Test;

[TestClass]
public class HardwareInfoTests
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
            ServiceName = "FakeConfig"

        });

        ConfigurationLoader.SetRuntimeConfiguration(runtimeconfig);

        Assert.IsTrue(runtimeconfig.Services.Any(x => x.ServiceName == "FakeConfig"));

        ConfigurationLoader.SetRuntimeConfiguration(ConfigurationLoader.CreateDefaultConfiguration());

    }
}
