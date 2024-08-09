using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Spark;
using Ahsoka.Spark;
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
using System.Linq;
using System.Net;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class SparkTeamsTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    readonly AutoResetEvent loginEvent = new(false);

    [TestMethod]
    public void TestSparkTeams()
    {
        SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out SparkTests.DefaultTestSparkServer);

        var loginDto = SparkAccountClient.SharedSparkClient.LoginToSpark(SparkTests.DefaultTestSparkServer,
                                 SparkEndPointType.Teams,
                                 SecurityGroopType.Account,
                                 identity, SecureLocalStrings.ToSecureString(password),
                                 LogoutCompleteEvent);

        Assert.IsNotNull(loginDto);

        string testTeam = "TestTeam" + Guid.NewGuid().ToString();

        // Team will create a new AccountID to represent the teams data / login
        var team = SparkProjectTools.AddTeam(new TeamDTO() { TeamName = testTeam });

        // Get the List of Teams and Find the Specific One.
        var teamsList = SparkProjectTools.GetTeams();
        Assert.IsTrue(teamsList.Count(x => x.TeamName == testTeam) == 1);

        // Add Team Member Must be Owner to Do This.
        SparkProjectTools.AddTeamMember(team.TeamID, "build@enovationcontrols.com");

        // Get Team List
        teamsList = SparkProjectTools.GetTeamMembers(team.TeamID);
        Assert.IsTrue(teamsList.Count(x => x.TeamName == testTeam) == 2);

        // Revoke the Team Member
        SparkProjectTools.RemoveTeamMember(team.TeamID, "build@enovationcontrols.com");

        // Check that Team List Changed
        teamsList = SparkProjectTools.GetTeamMembers(team.TeamID);
        Assert.IsTrue(teamsList.Count(x => x.TeamName == testTeam) == 1);

        // Test Artifacts and Device Keys
        TestDevKeys(team);

        // Test Containers
        var container = TestContainers(team);

        // Provision a "Fake" Display (use Local Service)
        ProvisionDisplayForContainer(team, container);

        TestPreProvision(team);

        // Delete Container
        DeleteContainer(team, container);

        // Remove the Test Team
        SparkProjectTools.RemoveTeam(team);

        // Validate that its Gone
        team = SparkAccountClient.SharedSparkClient.GetTeam(team.TeamID);
        Assert.IsTrue(team.TeamID == Guid.Empty);

        // Logout (Clear Tokens)
        SparkAccountClient.SharedSparkClient.RequestLogout();

        // Wait For Logout
        loginEvent.WaitOne(TimeSpan.FromSeconds(10));

    }

    private DeviceFactoryInfoDTO TestPreProvision(TeamDTO team)
    {
        SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out string url);

        SparkProvisioningClient.SharedSparkClient.LoginToSpark(url, SparkEndPointType.Provisioning, SecurityGroopType.Provisioning, identity, SecureLocalStrings.ToSecureString(password), null);

        Guid deviceId = Guid.Parse("734df717-700d-9374-25a5-dfc258215d9f");

        var newFactoryInfo = new DeviceFactoryInfoDTO()
        {
            DeviceID = deviceId,
            DeviceHardwareID = "004100273431511230393938",
            MacAddress = "",
            PartNumber = "78700725",
            SerialNumber = "45000544424",
            PlatformFamily = PlatformFamily.OpenViewLinux.ToString(),
            PlatformQualifier = "S70",
            FactoryInfo = JsonUtility.Serialize(HardwareInfo.GetHardwareInfo(PlatformFamily.OpenViewLinux, "S70")),
            DevicePublicKey = string.Empty,
        };

        var factoryResultVerification = SparkProvisioningClient.SharedSparkClient.CreateFactoryInfo(newFactoryInfo);

        bool isSaved = newFactoryInfo.SerialNumber == factoryResultVerification.SerialNumber &&
                      newFactoryInfo.PartNumber == factoryResultVerification.PartNumber &&
                      newFactoryInfo.MacAddress == factoryResultVerification.MacAddress &&
                      newFactoryInfo.DeviceHardwareID == factoryResultVerification.DeviceHardwareID &&
                      newFactoryInfo.DeviceID == factoryResultVerification.DeviceID &&
                      newFactoryInfo.DevicePublicKey == factoryResultVerification.DevicePublicKey;


        var progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

        // Test Decode
        var items = new List<ProvisionItem>();
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, Properties.Resources.DeviceList);
        var results = SparkCustomerTools.DecodeDeviceCSV(items, tempFile, progress);
        Assert.IsTrue(items.Count == 1);
        File.Delete(tempFile);

        items.Clear();
        var subInfo = new ProvisionDisplayRequest()
        {
            ApplicationId = team.TeamID,
            PackageName = "TestPackage"
        };

        //SparkOTA Module Info
        var module = SparkAccountClient.SharedSparkClient.GetModules().FirstOrDefault(x => x.Name.Contains("OTA"));
        subInfo.Licenses.Add(new ModuleLicense() { LicenseId = module.ID, LicenseName = module.Name });


        items.Add(new ProvisionItem(Enum.Parse<PlatformFamily>(newFactoryInfo.PlatformFamily),
                                    newFactoryInfo.PartNumber,
                                    newFactoryInfo.SerialNumber,
                                    newFactoryInfo.DeviceID.ToString()));

        var sparkLogin = new SparkConnectionInfo() { CurrentAuthorization = SparkAccountClient.SharedSparkClient.CurrentAuthorization, Url = SparkTests.DefaultTestSparkServer };

        try
        {
            SparkCustomerTools.PreProvisionDevices(null, sparkLogin, subInfo, progress, items);
            var device = SparkAccountClient.SharedSparkClient.GetTeamDevices(team.TeamID.ToString(), null).Devices.FirstOrDefault(x => x.DeviceID == newFactoryInfo.DeviceID) ;

            SparkAccountClient.SharedSparkClient.DeleteTeamDevice(device,team);
        }
        catch { }

        return factoryResultVerification;
    }

    private void ProvisionDisplayForContainer(TeamDTO team, ReleaseDTO container)
    {
        // Start System Service to Talk To.
        SparkService sparkService = new();
        SparkServiceClient sparkClient = new();
        AhsokaRuntime.Default.ConnectEndPoints(sparkService, sparkClient);

        var subscription = sparkClient.GetSparkSubscription();
        subscription.SubscriptionUrl = SparkTests.DefaultTestSparkServer;
        subscription.ReleaseName = "";

        var info = new TargetConnectionInfo() { PlatformFamily = PlatformFamily.Windows64, PlatformQualifier = "Desktop", HostName = "localhost", UserName = "" };
        var sparkLogin = new SparkConnectionInfo() { CurrentAuthorization = SparkAccountClient.SharedSparkClient.CurrentAuthorization, Url = SparkTests.DefaultTestSparkServer };

        var subInfo = new ProvisionDisplayRequest()
        {
            PackageName = container.ReleaseName,
            ApplicationId = team.TeamID
        };

        //SparkOTA Module Info
        var module = SparkAccountClient.SharedSparkClient.GetModules().FirstOrDefault(x => x.Name.Contains("OTA"));
        subInfo.Licenses.Add(new ModuleLicense() { LicenseId = module.ID, LicenseName = module.Name });

        var progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

        // Attempt to Provision our Display in Spark
        bool result = SparkCustomerTools.ProvisionDisplayInSpark(info, sparkLogin, subInfo, progress);

        var device = SparkProjectTools.GetTeamDevices(team.TeamID, null).Devices.First();
        SparkProjectTools.PromoteDevice(team, device);// Beta
        SparkProjectTools.PromoteDevice(team, device);// Develop

        // Force to Develop Group.
        device.GroopID = team.DevelopmentDeviceGroup;
        SparkAccountClient.SharedSparkClient.UpdateTeamDevice(device, team);
        device = SparkProjectTools.GetTeamDevices(team.TeamID, null).Devices.First();

        Thread.Sleep(1000);

        // Check for Releases
        string path = Path.GetTempFileName();
        SparkReleaseInfo releaseInfo = sparkClient.GetAvailableReleases();

        Assert.IsTrue(releaseInfo.Releases.Count > 0);

        var release = releaseInfo.Releases[0];

        if (releaseInfo.Releases.Count > 0)
        {
            // Download the Release
            var downloadInfo = sparkClient.DownloadApplication(new SparkReleaseDownloadRequest()
            {
                ReleaseId = releaseInfo.Releases.First().ReleaseId,
                PathToFile = path,
                TimeoutInSeconds = 20
            });

            Assert.IsTrue(downloadInfo.Status == MessageReplyStatus.Success);

            // Cleanup File.
            File.Delete(path);
        }
        SparkProjectTools.RemoveTeamDevice(team, device);

        AhsokaRuntime.Default.DisconnectAllEndPoints();

    }

    private ReleaseDTO TestContainers(TeamDTO team)
    {
        // Create temp package for Upload
        var package = PackagingTests.CreatePackage(out string tempFilePath);

        // Set App ID for our Team
        package.ApplicationId = team.TeamID;

        var uploadInfo = new ReleaseDTO
        {
            ContainerVersion = "1.0",
            ContainerType = SparkContainerType.Software,
            TrackType = SparkTrackType.Development,
            ReleaseName = package.PackageName,
            ChannelName = SparkCustomerTools.CreateChannelName(team.TeamName, package.ApplicationId.ToString()),
            // Add Item to Upload
            Contents = new()
            {
                new ReleaseFileDTO()
                {
                    Path = tempFilePath
                }
            }
        };


        var progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

        SparkCustomerTools.UploadToSpark(package, uploadInfo, team, progress);

        var version = SparkCustomerTools.UpdateVersion(team, uploadInfo);

        return uploadInfo;
    }

    private static void DeleteContainer(TeamDTO team, ReleaseDTO uploadInfo)
    {

        // Get Containers and Validate its Uploadex
        var containers = SparkProjectTools.GetTeamContainers(team.TeamID, null);
        Assert.IsTrue(containers.Containers.First().ContainerName == uploadInfo.ContainerName);

        SparkProjectTools.PromoteApplication(team, containers.Containers.First());
        SparkProjectTools.DemoteApplication(team, containers.Containers.First());

        SparkProjectTools.RemoveTeamContainer(team, containers.Containers.First());
        containers = SparkProjectTools.GetTeamContainers(team.TeamID, null);
        Assert.IsTrue(containers.Containers.Count == 0);


    }

    private static void TestDevKeys(TeamDTO team)
    {
        // Test Development Keys via Team Tools Interface
        // Create New Key
        string keyName = "TestFile";
        var keysFiles = SshUtility.CreateKeyFile(keyName);

        SparkProjectTools.CreateDevelopmentKey(team, keyName, KeyTypes.DeviceKey);
        var artifacts = SparkProjectTools.GetTeamDevelopmentKeys(team);
        Assert.IsTrue(artifacts.First().ArtifactName.StartsWith(keyName)); // Should be One Artifact now.

        // Remove Local Key so we can Attempt the Restoration Process.
        if (File.Exists(keysFiles.PublicKeyPath))
            File.Delete(keysFiles.PublicKeyPath);

        if (File.Exists(keysFiles.PrivateKeyPath))
            File.Delete(keysFiles.PrivateKeyPath);

        SparkProjectTools.CreateDevelopmentKey(team, keyName, KeyTypes.DeviceKey);

        // Delete the DevKeys Online and Local
        SparkProjectTools.RemoveDevelopmentKey(team, artifacts.First());

        // Remove Local Key so we can Attempt the Restoration Process.
        if (File.Exists(keysFiles.PublicKeyPath))
            File.Delete(keysFiles.PublicKeyPath);
        else
            Assert.Fail("Key was not Downloded");

        if (File.Exists(keysFiles.PrivateKeyPath))
            File.Delete(keysFiles.PrivateKeyPath);
        else
            Assert.Fail("Key was not Downloded");

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
}
