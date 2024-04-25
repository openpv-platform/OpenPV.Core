#pragma warning disable CS1591
using Ahsoka.DeveloperTools.Spark;
using Ahsoka.System;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Ahsoka.Spark;

public static class SparkProjectTools
{
    internal const string deviceKey = "_DeviceKey";
    internal const string appCert = "_AppCert";

    public static bool Login(string url, string login, string password)
    {
        try
        {
            _ = SparkAccountClient.SharedSparkClient.LoginToSpark(url, SparkEndPointType.Teams, SecurityGroopType.Account, login, SecureLocalStrings.ToSecureString(password), null, "Ahsoka Client Tools");

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static TeamDTOCollection GetTeams()
    {
        // Fetch This Channel
        return SparkAccountClient.SharedSparkClient.GetTeams();
    }

    public static TeamDTO AddTeam(TeamDTO addTeamDTO)
    {
        // Fetch This Channel
        return SparkAccountClient.SharedSparkClient.CreateTeam(addTeamDTO.TeamName);
    }

    public static string RemoveTeam(TeamDTO teamDto)
    {
        // Fetch This Channel
        return SparkAccountClient.SharedSparkClient.DeleteTeam(teamDto.TeamID);
    }

    public static TeamDTOCollection GetTeamMembers(Guid teamID)
    {
        return SparkAccountClient.SharedSparkClient.GetTeamMembers(teamID);
    }

    public static ContainerSearchResultsDTO GetTeamContainers(Guid teamID, ClientContainerSearchDTO searchInfo)
    {
        return SparkAccountClient.SharedSparkClient.GetTeamContainers(teamID.ToString(),searchInfo);
    }

    public static TeamDTO AddTeamMember(Guid teamID, string emailAddress)
    {
        return SparkAccountClient.SharedSparkClient.InviteTeamMember(teamID, emailAddress);
    }

    public static string RemoveTeamMember(Guid teamID, string emailAddress)
    {
        return SparkAccountClient.SharedSparkClient.RevokeTeamMember(teamID, emailAddress);
    }

    public static void PromoteApplication(TeamDTO team, ContainerDTO container)
    {
        switch (container.TrackType)
        {
            case SparkTrackType.Development:
                container.TrackType = SparkTrackType.Beta;
                break;

            case SparkTrackType.Beta:
            case SparkTrackType.Canary:
                container.TrackType = SparkTrackType.Release;
                break;

            default:
                return;
        }

        SparkAccountClient.SharedSparkClient.UpdateTeamContainer(container, team);
    }

    public static void DemoteApplication(TeamDTO team, ContainerDTO container)
    {
        switch (container.TrackType)
        {
            case SparkTrackType.Release:
                container.TrackType = SparkTrackType.Beta;
                break;

            case SparkTrackType.Beta:
            case SparkTrackType.Canary:
                container.TrackType = SparkTrackType.Development;
                break;

            default:
                return;
        }

        SparkAccountClient.SharedSparkClient.UpdateTeamContainer(container, team);

    }

    public static void RemoveTeamContainer(TeamDTO team, ContainerDTO container)
    {
        SparkAccountClient.SharedSparkClient.DeleteTeamContainer(container, team);
    }

    public static DeviceSearchResultsDTO GetTeamDevices(Guid teamID, DeviceSearchDTO searchInfo)
    {
        return SparkAccountClient.SharedSparkClient.GetTeamDevices(teamID.ToString(), searchInfo);
    }

    public static void RemoveTeamDevice(TeamDTO team, DeviceDTO device)
    {
        SparkAccountClient.SharedSparkClient.DeleteTeamDevice(device, team);
    }

    public static void DemoteDevice(TeamDTO team, DeviceDTO device)
    {
        if (device.GroopID == team.BetaDeviceGroup)
            device.GroopID = team.ReleaseDeviceGroup;
        else if (device.GroopID == team.DevelopmentDeviceGroup)
            device.GroopID = team.BetaDeviceGroup;
        else
            return;

        SparkAccountClient.SharedSparkClient.UpdateTeamDevice(device, team);
    }

    public static void PromoteDevice(TeamDTO team, DeviceDTO device)
    {
        if (device.GroopID == team.ReleaseDeviceGroup)
            device.GroopID = team.BetaDeviceGroup;
        else if (device.GroopID == team.BetaDeviceGroup)
            device.GroopID = team.DevelopmentDeviceGroup;
        else
            return;

        SparkAccountClient.SharedSparkClient.UpdateTeamDevice(device, team);
    }

    public static void RenameDevice(TeamDTO team, DeviceDTO device)
    {
        SparkAccountClient.SharedSparkClient.UpdateTeamDevice(device, team);

    }

    public static string RemoveDevelopmentKey(TeamDTO team, ArtifactDTO artifact)
    {
        return SparkAccountClient.SharedSparkClient.DeleteArtifact(team.TeamID, artifact.ArtifactName, artifact.ArtifactGroup);
    }

    public static ArtifactDTOCollection GetTeamDevelopmentKeys(TeamDTO team)
    {
        return SparkAccountClient.SharedSparkClient.GetArtifacts(team.TeamID, "", "SecurityData");
    }

    public static void CreateDevelopmentKey(TeamDTO team, string keyName, KeyTypes artifactType)
    {
        string artifactName;

        artifactName = GetArtifactName(keyName, artifactType);
       
        // Generate Artifact Request (Upload or Download
        ArtifactDTOCollection artifactRequest = new()
        {
            new ArtifactDTO() { ArtifactGroup = "SecurityData", TeamID = team.TeamID, ArtifactName = artifactName, ArtifactCreator = team.MemberEmailAddress }
        };
       
        var keyFile = GetTeamDevelopmentKeys(team);

        if (!keyFile.Any(x => x.ArtifactName == artifactName))
        {
            // Create New Key
            DeviceKeyInfo key = artifactType == KeyTypes.DeviceKey ? 
                GetUploadInfo(keyName) :   // Generates an ssh key
                GetCertInfo(keyName);      // Generates a key pair stored encrypted in cert folder. 

            var uploadTokens = SparkAccountClient.SharedSparkClient.GetArtifactUploadTokens(team.TeamID, artifactRequest);

            // Serialize to text
            string result = JsonUtility.Serialize(key);

            // Upload Key 
            var publicKeyToken = uploadTokens.FirstOrDefault(x => x.ArtifactName == artifactName);
            SparkAccountClient.SharedSparkClient.UploadDTO(publicKeyToken, new MemoryStream(UTF8Encoding.UTF8.GetBytes(result)));
        }
        else
        {
            // Check if Keys are Already Local...if not download them directly into their correct location.
            bool isLocal = false;
            string certFilePath = null;
            KeyFileInfo deviceKeyInfo = null;
            if (artifactType == KeyTypes.DeviceKey)
            {
                deviceKeyInfo = SshUtility.GetKeyFileNames(keyName);
                isLocal = deviceKeyInfo.IsLocal;
            }
            else
            {
                certFilePath = GetCertPath(keyName);
                isLocal = File.Exists(certFilePath);
            }


            if (!isLocal)
            {
                var downloadTokens = SparkAccountClient.SharedSparkClient.GetArtifactDownloadTokens(team.TeamID, artifactRequest);

                using var stream = new MemoryStream();
                SparkAccountClient.SharedSparkClient.DownloadDTO(downloadTokens.First(), stream);

                DeviceKeyInfo info = JsonUtility.Deserialize<DeviceKeyInfo>(Encoding.UTF8.GetString(stream.ToArray()));

                if (artifactType == KeyTypes.DeviceKey)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(deviceKeyInfo.PublicKeyPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(deviceKeyInfo.PublicKeyPath));

                    File.WriteAllText(deviceKeyInfo.PrivateKeyPath, info.PrivateKey);
                    File.WriteAllText(deviceKeyInfo.PublicKeyPath, info.PublicKey);
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(certFilePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(certFilePath));

                    StringEncryption.CreateSecureFile(certFilePath, JsonUtility.Serialize(info));
                }
            }
        }
    }

    private static DeviceKeyInfo GetCertInfo(string keyName)
    {
        // Generate Key Pair
        var keygen = new SshKeyGenerator(2048);
        string pathToCerts = GetCertPath(keyName);

        if (!Directory.Exists(Path.GetDirectoryName(pathToCerts)))
            Directory.CreateDirectory(Path.GetDirectoryName(pathToCerts));

        var keyData = new DeviceKeyInfo()
        {
            PublicKey = keygen.ToPublicKey(),
            PrivateKey = keygen.ToRfcPublicKey()
        };

        StringEncryption.CreateSecureFile(pathToCerts, JsonUtility.Serialize(keyData));

        return keyData;
    }

    public static string GetCertPath(string keyName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AhsokaData", "AhsokaCerts", keyName + ".cert");
    }

    private static DeviceKeyInfo GetUploadInfo(string keyName)
    {
        KeyFileInfo keysFiles = SshUtility.CreateKeyFile(keyName);

        // Create Key Data
        return new DeviceKeyInfo()
        {
            PublicKey = File.ReadAllText(keysFiles.PublicKeyPath),
            PrivateKey = File.ReadAllText(keysFiles.PrivateKeyPath)
        };
    }

    public static string GetArtifactName(string keyName, KeyTypes type)
    {
        string deviceKeyName = GetKeyName(keyName);

        if (type == KeyTypes.DeviceKey)
            return $"{deviceKeyName}{deviceKey}";
        else
            return $"{deviceKeyName}{appCert}";
    }

    public static string GetKeyName(string keyName)
    {
        string deviceKeyName = $"{keyName}";
        deviceKeyName = Regex.Replace(deviceKeyName, @"[^a-zA-Z0-9]+", string.Empty);
        return deviceKeyName;
    }

    public class DeviceKeyInfo
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }
}

public enum KeyTypes
{
    DeviceKey,
    AppCert
}
