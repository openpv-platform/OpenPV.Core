#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Spark;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Spark;

public static class SparkCustomerTools
{
   
    public static bool ProvisionDisplayInSpark(ITargetConnectionInfo connectionInfo,
                                                SparkConnectionInfo sparkInfo,
                                                IProvisionDisplayRequest provisioningInfo,
                                                IProgress<string> progress, string deviceDescription = null)
    {

        sparkInfo.LoginToTeamClient();

        // Get the Team that we will be assigning this Display To
        var teamList = SparkAccountClient.SharedSparkClient.GetTeams();
        var teamDto = teamList.FirstOrDefault(x => x.TeamID.ToString().StartsWith(provisioningInfo.ApplicationId.ToString()));
        if (teamDto == null)
        {
            progress.Report($"Unknown Project (ApplicationID): {provisioningInfo.ApplicationId} - Required for Provisioning");
            return false;
        }

        // Create Connection to the Display
        var client = CreateClient(connectionInfo.HostName);

        // Get Current Subscription Data (Returns Key and DeviceID)
        var sub = client.GetSparkSubscription(3000);
        if (sub == null)
        {
            progress.Report("Spark Service was unable to provision the display.");
            progress.Report("Please verify you are running the Installer UI or an Application which includes Spark Services.");
            return false;
        }

        // Add OTA Subscription
        var sparkSubscription = new SparkSubscription()
        {
            DeviceId = sub.DeviceId,
            SubscriptionUrl = sparkInfo.Url,
            ChannelName = CreateChannelName(teamDto.TeamName, provisioningInfo.ApplicationId.ToString()),
            ReleaseName = provisioningInfo.PackageName
        };

        progress.Report("Subscribing Display To Spark Channel.");
        client.SetSparkSubscription(sparkSubscription);
        sub = client.GetSparkSubscription(3000);

        progress.Report($"Provisioning Display {sub.DeviceId} for Channel:{CreateChannelName(teamDto.TeamName, provisioningInfo.ApplicationId.ToString())}");

        try
        {
            var licenseInfo = PushSparkLicenseInfo(sub, teamDto.TeamID, teamDto.ReleaseDeviceGroup, provisioningInfo, progress, false);
            
            // Push License List to Display
            progress.Report("Loading License File onto Target");
            client.UpdateLicenseInfo(licenseInfo);
        }
        catch (Exception ex)
        {
            progress.Report($"Provisioning Failed With Error: {ex.Message}");
            return false;
        }
        finally
        {
            // Shut Down Client and Runtime
            client.CurrentRuntime.RequestShutdown();
            client.CurrentRuntime.WaitForShutdown();
        }

        return true;
    }

    internal static ModuleLicenseInfo PushSparkLicenseInfo(SparkSubscriptionInfo subInfo,
                                    Guid teamID,
                                    Guid groupID,
                                    IProvisionDisplayRequest provisioningInfo,
                                    IProgress<string> progress,
                                    bool isFactory)
    {
        // Create License List Request
        var list = new ModuleLicenseList()
        {
            TeamId = provisioningInfo.ApplicationId,
            DeviceId = subInfo.DeviceId,
            Licenses = new List<ModuleLicense>(provisioningInfo.Licenses)
        };

        var deviceDto = new DeviceDTO()
        {
            DeviceID = Guid.Parse(subInfo.DeviceId),
            GroopID = groupID,
            Description = $"Device ({subInfo.DeviceId.ToString().Substring(0, 8)})",
            PartNumber = subInfo.PartNumber,
            SerialNumber = subInfo.SerialNumber,
            Key = subInfo.DeviceKey,
            UpdateKey = true,
            IsFactory = isFactory,
            UseDeviceIDForLookup = true
        };

        // Get Module Info and Check if Valid
        progress.Report("Sending License List to Spark for Signing.");
        ProvisioningResult result = SparkAccountClient.SharedSparkClient.ProvisionDeviceLicenses(teamID.ToString(), subInfo.DeviceId, new ProvisioningInfo() { ModuleLicenseList = list, DeviceInfo = deviceDto});

        if (!RSAEncryption.VerifyDataSignature(result.ModuleLicenseInfo.Signature, result.ModuleLicenseInfo.LicenseData, Properties.Resources.ModuleLicensePublicKeyPEM))
        {
            progress.Report($"Provisioning failed due to invalid signature for license file");
            return null;
        }

        // Throw an Error if the License List was not accepted....aka returned list does not match
        var responseList = ProtoBuf.Serializer.Deserialize<ModuleLicenseList>(new MemoryStream(result.ModuleLicenseInfo.LicenseData));
        foreach (var item in list.Licenses)
            if (!responseList.Licenses.Any(x => x.LicenseId == item.LicenseId))
            {
                progress.Report($"Provisioning failed to return a license for Module: {item.LicenseName}");
                return null;
            }

        // Check if OTA License is Included
        if (result.DeviceInfo != null)
        {        
            progress.Report($"Device Provisioned to Group - {result.DeviceInfo.GroopName}");
        }

        return result.ModuleLicenseInfo;
    }

    public static bool UploadToSpark(PackageInformation packageInfo, ReleaseDTO uploadInfo, TeamDTO team, IProgress<string> progress)
    {

        // Update Container name
        uploadInfo.ContainerName = uploadInfo.ReleaseName + " " + uploadInfo.ContainerVersion;

        // File Names
        string outputFilePath = uploadInfo.Contents.First().Path;
        string outputFileName = Path.GetFileName(outputFilePath);

        // Fixup Path
        uploadInfo.Contents.First().Path = outputFileName;

        // Create Spark Upload Area
        string uploadArea = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string uploadFilePath = Path.Combine(uploadArea, outputFileName);
        Directory.CreateDirectory(uploadArea);
        File.Copy(outputFilePath, uploadFilePath);

        // Write Manifest
        string manifest = JsonUtility.Serialize(uploadInfo);
        string manifestFileName = Path.Combine(uploadArea, "SparkUpload.release.json");
        File.WriteAllText(manifestFileName, manifest);

        // Needed to Transform from Spark Status to Console Output
        var progressLocal = new Progress<SparkUploadStatusInfo>(progressInfo =>
        {
            if (progressInfo.Percent is > 0 and < 100)
                progress.Report($"{progressInfo.Description} {progressInfo.Percent}% Complete");
            else
                progress.Report($"{progressInfo.TitleText}");

        });

        // Crate Container and Upload
        var container = SparkAccountClient.SharedSparkClient.CreateContainerData(uploadArea, new string[] { manifestFileName }, uploadInfo.ContainerVersion);
        SparkAccountClient.SharedSparkClient.UploadContainer(container, team, progressLocal);

        return true;
    }

    static SparkServiceClient CreateClient(string hostName)
    {
        var config = new System.ServiceConfiguration()
        {
            ServiceName = "SparkService",
            TcpConnectionAddress = hostName,
            DataChannel = 5002,
        };

        var client = new SparkServiceClient(config);
        AhsokaRuntime.CreateBuilder()
                     .AddClients(client)
                     .StartLocalRuntime();

        return client;
    }

    public static ReleaseDTO UpdateVersion(TeamDTO team, ReleaseDTO uploadInfo)
    {
        // Fetch This Channel
        var channels = SparkAccountClient.SharedSparkClient.GetTeamContainers(team.TeamID.ToString(), uploadInfo.ChannelName);
        if (channels.Count > 0)
        {
            try // Attempt to Sort by Version if we can.
            {
                channels.Containers.Sort((container1, container2) => new Version(container1.ContainerVersion).CompareTo(new Version(container2.ContainerVersion)));
            }
            catch { channels.Containers.Sort((container1, container2) => container1.ContainerVersion.CompareTo(container2.ContainerVersion)); }

            var container = channels.Containers.Last();
            var version = new Version(container.ContainerVersion);

            if (version.Build > -1)
                uploadInfo.ContainerVersion = $"{version.Major}.{version.Minor}{version.Build + 1}";
            else if (version.Minor > -1)
                uploadInfo.ContainerVersion = $"{version.Major}.{version.Minor + 1}";
        }

        return uploadInfo;
    }

    public static string CreateChannelName(string teamName, string applicationId)
    {
        return teamName + " (" + applicationId + ")";
    }
}
