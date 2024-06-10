using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Test;

[TestClass]
public class LicenseTests
{
    public TestContext TestContext { get; set; }

    readonly string testLicenseID = Guid.NewGuid().ToString();
    readonly string testLicenseName = "TestLicense";

    [TestMethod]
    public void TestLicenseManager()
    {
        var keys = RSAEncryption.GenerateNewKeyPair();

        // Capture LicenseID to restore later
        var oldLicenseID = SystemInfo.HardwareInfo.FactoryInfo.DeviceId;
        SystemInfo.HardwareInfo.FactoryInfo.DeviceId = Guid.Parse(testLicenseID);

        // Simulate License Values
        var licenseInfo = new Dictionary<string, string>
        {
            [testLicenseID] = testLicenseName
        };

        // Create License File with local key
        var data = ModuleLicenseManager.CreateLicenseFile(keys.Key, testLicenseID, licenseInfo);
        string fileName = SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(System.Hardware.FactoryPaths.ModuleLicenseFile);
        File.WriteAllBytes(fileName, data.ToArray());

        // Now Lets Test it with our Local keys
        ModuleLicenseManager.InitWithPublicKey(keys.Value);
        var isLicensedResult = ModuleLicenseManager.IsLicensed(testLicenseID);
        var licenseID = ModuleLicenseManager.GetLicenseID(testLicenseName);

        Assert.IsTrue(isLicensedResult);
        Assert.IsTrue(licenseID == testLicenseID);

        SystemInfo.HardwareInfo.FactoryInfo.DeviceId = oldLicenseID;
    }
}
