using Ahsoka.Spark;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using EnovationCloud.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class SparkTests : LinearTestBase
{
    public static string DefaultTestSparkServer = "sparkservicedev.azurewebsites.net";

    public TestContext TestContext { get; set; }

    readonly AutoResetEvent loginEvent = new(false);

    [TestMethod]
    public void TestSparkLogin()
    {
        SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out DefaultTestSparkServer);

        var loginDto = SparkAccountClient.SharedSparkClient.LoginToSpark(DefaultTestSparkServer,
                                 SparkEndPointType.Teams,
                                 SecurityGroopType.Account,
                                 identity, SecureLocalStrings.ToSecureString(password),
                                 LogoutCompleteEvent);

        Assert.IsNotNull(loginDto);

        var subscriptions = SparkAccountClient.SharedSparkClient.GetSubscribedReleases();
        Assert.IsNotNull(subscriptions);

        var account = SparkAccountClient.SharedSparkClient.GetAccount();
        Assert.IsNotNull(account);

        var releases = SparkAccountClient.SharedSparkClient.GetSparkClientReleases();
        Assert.IsNotNull(releases);

        var availableReleases = SparkAccountClient.SharedSparkClient.GetAvailableReleases();
        Assert.IsNotNull(availableReleases);

        // Login with Refresh token
        var newToken = SparkAccountClient.SharedSparkClient.LoginWithRefreshToken(DefaultTestSparkServer, loginDto);

        // Test Tokens
        var tokenInfo = new TokenValidationInfo()
        {
            Audience = "Test",
            AuthorizedRole = 1,
            Features = "Test, Test2",
            LicenseExpires = DateTime.UtcNow.AddDays(1),
            Name = "Test",
            NameIdentifier = Guid.NewGuid(),
            SecurityGroup = SecurityGroopType.Account,
            TokenType = TokenType.AccessToken
        };

        SparkAccountClient.SharedSparkClient.IsRefreshTokenStillValid(newToken);

        // Logout (Clear Tokens)
        SparkAccountClient.SharedSparkClient.RequestLogout();

        // Wait For Logout
        loginEvent.WaitOne(TimeSpan.FromSeconds(10));
    }

    [TestMethod]
    public void TestSparkSecurityUtilities()
    {
        string mystring = "MyString";
        var secureString = SecureLocalStrings.ToSecureString(mystring);
        var secureString2 = SecureLocalStrings.ToSecureString(mystring);
        var insecureString = SecureLocalStrings.ToInsecureString(secureString);
        bool result = SecureLocalStrings.IsEqualTo(secureString, secureString2);

        Assert.IsTrue(result);
        Assert.IsTrue(mystring == insecureString);
    }

    [TestMethod]
    public void TestSparkDownload()
    {
        SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out SparkTests.DefaultTestSparkServer);

        SparkConnectionInfo info = new()
        {
            Url = "sparkserviceprod.azurewebsites.net",
            Login = identity,
            Password = password
        };

        string tmpFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        SparkTools tools = new();
        tools.Download(info.Url, info.Login, info.Password, tmpFolder, "OpenPV Dependencies", "1.254.0", "Windows64_TargetSupport.zip");

        Assert.IsNotNull(Directory.GetFiles(tmpFolder).FirstOrDefault(x => x.EndsWith("Windows64_TargetSupport.zip")));

        Directory.Delete(tmpFolder, true);
    }

    public void LogoutCompleteEvent(WebException ex)
    {
        loginEvent.Set();
    }

    [TestMethod]
    public void TestSparkDeviceCredentials()
    {
        Guid deviceId = Guid.NewGuid(); // Long Part Number / SN with Mixed Text / Numbers and leading 0

        // Genearte Fingerprint
        string deviceFingerPrint = SecurityUtilities.GenerateFingerPrint(deviceId);

        bool result = SecurityUtilities.TryDecodeFingerprint(deviceFingerPrint, out Guid resultDeviceId);

        Assert.IsTrue(result);
        Assert.AreEqual(deviceId, resultDeviceId);
    }

    [TestMethod]
    public void FactoryInfoTest()
    {
        SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out SparkTests.DefaultTestSparkServer);

        var authDTO = SparkProvisioningClient.SharedSparkClient.LoginToSpark(
            DefaultTestSparkServer,
            SparkEndPointType.Provisioning,
            SecurityGroopType.Provisioning,
            identity,
            SecureLocalStrings.ToSecureString(password),
            null);

        // Create Device
        var deviceDto = new DeviceFactoryInfoDTO()
        {
            DeviceID = Guid.NewGuid(),
            PlatformFamily = "OpenViewLinux",
            PlatformQualifier = "S50",
            SerialNumber = "23232323",
            PartNumber = "32323232",
            MacAddress = "AA:BB:CC:DD",
            FactoryInfo = "{JsonStuff}",
            DeviceHardwareID = Guid.NewGuid().ToString(),
        };

        try
        {
            var retVal = SparkProvisioningClient.SharedSparkClient.CreateFactoryInfo(deviceDto);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.InnerException);
        }

        var teamDTO = SparkAccountClient.SharedSparkClient.LoginToSpark(
          DefaultTestSparkServer,
          SparkEndPointType.Teams,
          SecurityGroopType.Account,
          identity,
          SecureLocalStrings.ToSecureString(password),
          null);

        try
        {
            var getResult = SparkAccountClient.SharedSparkClient.GetFactoryInfo(deviceDto.PlatformFamily, deviceDto.DeviceHardwareID);

            Assert.AreEqual(getResult.PlatformFamily, deviceDto.PlatformFamily);
            Assert.AreEqual(getResult.DeviceHardwareID, deviceDto.DeviceHardwareID);
            Assert.AreEqual(getResult.SerialNumber, deviceDto.SerialNumber);
            Assert.AreEqual(getResult.PartNumber, deviceDto.PartNumber);
            Assert.AreEqual(getResult.MacAddress, deviceDto.MacAddress);
            Assert.AreEqual(getResult.FactoryInfo, deviceDto.FactoryInfo);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.InnerException);
        }

        try
        {
            SparkProvisioningClient.SharedSparkClient.RemoveFactoryInfo(deviceDto.PlatformFamily, deviceDto.DeviceHardwareID);

            // Result should be empty
            var getResult = SparkAccountClient.SharedSparkClient.GetFactoryInfo(deviceDto.PlatformFamily, deviceDto.DeviceHardwareID);
            Assert.AreEqual(getResult.DeviceID, Guid.Empty);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.InnerException);
        }
    }
}
