using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Test;

[TestClass]
public class LicenseTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    readonly string testLicenseID = Guid.NewGuid().ToString();
    readonly string testLicenseName = "TestLicense";

    [TestMethod]
    public void TestLicenseManager()
    {
        string fileName = SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(System.Hardware.FactoryPaths.ModuleLicenseFile);
        if (File.Exists(fileName))
            File.Delete(fileName);

        var isLicensedResult = ModuleLicenseManager.IsLicensed(testLicenseID);
        Assert.IsFalse(isLicensedResult);

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
        File.WriteAllBytes(fileName, data.ToArray());

        // Now Lets Test it with our Local keys
        ModuleLicenseManager.InitWithPublicKey(keys.Value);
        isLicensedResult = ModuleLicenseManager.IsLicensed(testLicenseID);
        var licenseID = ModuleLicenseManager.GetLicenseID(testLicenseName);

        Assert.IsTrue(isLicensedResult);
        Assert.IsTrue(licenseID == testLicenseID);

        SystemInfo.HardwareInfo.FactoryInfo.DeviceId = oldLicenseID;
    }
   
    /// <summary>
    /// Test code for when need to add factory data for a unit already in the field
    /// Requires admin or DeviceProvisioning
    /// </summary>
    //[TestMethod]
    public void FactoryPrepInternal()
    {
        SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out string url);

        SparkProvisioningClient.SharedSparkClient.LoginToSpark("enovation.powervisionspark.com", SparkEndPointType.Provisioning, SecurityGroopType.Provisioning, identity, SecureLocalStrings.ToSecureString(password), null);

        Guid deviceId = Guid.Parse("734df717-700d-9374-25a5-dfc258215d9e");

        var newFactoryInfo = new DeviceFactoryInfoDTO()
        {
            DeviceID = deviceId,
            DeviceHardwareID = "004100273431511230393938",
            MacAddress = "",
            PartNumber = "78700725",
            SerialNumber = "45000544424",
            PlatformFamily = PlatformFamily.OpenViewLinux.ToString(),
            PlatformQualifier = "S70",
            FactoryInfo = JsonUtility.Serialize(HardwareInfo.GetHardwareInfo(PlatformFamily.OpenViewLinux,"S70")),
            DevicePublicKey = string.Empty,
        };

        var factoryResultVerification = SparkProvisioningClient.SharedSparkClient.CreateFactoryInfo(newFactoryInfo);

        bool isSaved = newFactoryInfo.SerialNumber == factoryResultVerification.SerialNumber &&
                      newFactoryInfo.PartNumber == factoryResultVerification.PartNumber &&
                      newFactoryInfo.MacAddress == factoryResultVerification.MacAddress &&
                      newFactoryInfo.DeviceHardwareID == factoryResultVerification.DeviceHardwareID &&
                      newFactoryInfo.DeviceID == factoryResultVerification.DeviceID &&
                      newFactoryInfo.DevicePublicKey == factoryResultVerification.DevicePublicKey;

    }
}
