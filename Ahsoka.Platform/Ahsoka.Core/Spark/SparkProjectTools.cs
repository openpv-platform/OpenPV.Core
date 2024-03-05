#pragma warning disable CS1591
using Ahsoka.System;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ahsoka.Spark;

public static class SparkProjectTools
{
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


    public static void CreateDevelopmentKey(TeamDTO team, string keyName)
    {
        KeyFileInfo keysFiles = null;
        string artifactName;

        artifactName = GetArtifactName(keyName);

        var keyFile = GetTeamDevelopmentKeys(team);

        if (!keyFile.Any(x => x.ArtifactName == artifactName))
        {
            // Create New Key
            keysFiles = SshUtility.CreateKeyFile(keyName);

            // Generate Upload Tokens
            ArtifactDTOCollection artifactToUpload = new()
            {
                new ArtifactDTO() { ArtifactGroup = "SecurityData", TeamID = team.TeamID, ArtifactName = artifactName, ArtifactCreator = team.MemberEmailAddress }
            };
            var uploadTokens = SparkAccountClient.SharedSparkClient.GetArtifactUploadTokens(team.TeamID, artifactToUpload);

            // Create Key Data
            DeviceKeyInfo key = new()
            {
                PublicKey = File.ReadAllText(keysFiles.PublicKeyPath),
                PrivateKey = File.ReadAllText(keysFiles.PrivateKeyPath)
            };

            // Serialize to text
            string result = JsonUtility.Serialize(key);

            // Upload Key 
            var publicKeyToken = uploadTokens.FirstOrDefault(x => x.ArtifactName == artifactName);
            SparkAccountClient.SharedSparkClient.UploadDTO(publicKeyToken, new MemoryStream(UTF8Encoding.UTF8.GetBytes(result)));
        }
        else
        {
            // Check if Keys are Already Local...if not download them directly into their correct location.
            keysFiles = SshUtility.GetKeyFileNames(keyName);

            if (!File.Exists(keysFiles.PublicKeyPath) || !File.Exists(keysFiles.PrivateKeyPath))
            {
                ArtifactDTOCollection artifactsToUpload = new()
                {
                    new ArtifactDTO() { ArtifactGroup = "SecurityData", TeamID = team.TeamID, ArtifactName = artifactName, ArtifactCreator = team.MemberEmailAddress }
                };
                var downloadTokens = SparkAccountClient.SharedSparkClient.GetArtifactDownloadTokens(team.TeamID, artifactsToUpload);

                using var stream = new MemoryStream();
                SparkAccountClient.SharedSparkClient.DownloadDTO(downloadTokens.First(), stream);

                DeviceKeyInfo info = JsonUtility.Deserialize<DeviceKeyInfo>(UTF8Encoding.UTF8.GetString(stream.ToArray()));

                if (!Directory.Exists(Path.GetDirectoryName(keysFiles.PublicKeyPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(keysFiles.PublicKeyPath));

                File.WriteAllText(keysFiles.PrivateKeyPath, info.PrivateKey);
                File.WriteAllText(keysFiles.PublicKeyPath, info.PublicKey);
            }
        }
    }

    public static string GetArtifactName(string keyName)
    {
        string deviceKeyName = GetKeyName(keyName);

        return $"{deviceKeyName}_DeviceKey";
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


