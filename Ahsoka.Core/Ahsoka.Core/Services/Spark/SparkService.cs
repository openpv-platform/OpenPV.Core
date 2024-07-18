#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.Spark;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using EnovationCloud.Core.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Services.Spark;

[AhsokaService(Name)]
public class SparkService : AhsokaServiceBase<SparkMessageTypes.Ids>
{
    #region Fields
    CancellationTokenSource source = new();
    string autoProvisionAddress = "enovation.powervisionspark.com";
    public const string Name = "SparkService";
    static readonly string deviceSigningKey = SystemInfo.HardwareInfo.GetDeviceSigningKey();
    #endregion

    #region Methods
    public SparkService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
    }

    public SparkService(ServiceConfiguration config) :
        base(config, new SparkServiceMessages())
    {
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<SparkMessageTypes.Ids>())
        {

            case SparkMessageTypes.Ids.SetSparkSubscription:

                HandleSetSparkSubscription(messageHeader, message as SparkSubscription);
                break;

            case SparkMessageTypes.Ids.GetSparkSubscription:

                HandleGetSparkSubscription(messageHeader);
                break;

            case SparkMessageTypes.Ids.GetAvailableReleases:

                HandleGetSparkReleases(messageHeader);
                break;

            case SparkMessageTypes.Ids.GetRelease:

                source = new CancellationTokenSource();
                HandleGetRelease(messageHeader, message as SparkReleaseDownloadRequest, source);
                break;

            case SparkMessageTypes.Ids.UpdateLicenseInfo:

                HandleUpdateLicenseFile(messageHeader, message as ModuleLicenseInfo);
                break;

            case SparkMessageTypes.Ids.SubscribeToAppChange:
                HandleSubscribeToAppChange(messageHeader);
                break;

            case SparkMessageTypes.Ids.UnsubscribeToAppChange:
                HandleUnsubscribeToAppChange(messageHeader);
                break;
        }
    }

    private void HandleSubscribeToAppChange(AhsokaMessageHeader messageHeader) 
    {
        SparkSubscriptionInfo info = GetSparkSubscription();

        var privateKey = SystemInfo.HardwareInfo.GetDeviceSigningKey(false);

        Console.WriteLine("Getting Auth Token");
        var auth = GetDeviceAuthorizationToken(info.SubscriptionUrl, Guid.Parse(info.DeviceId), privateKey);
        if (auth == null)
            throw new ApplicationException("No authorization found");

        //SparkDisplayClient.SharedSparkClient.ReceiveAppChanges = new Action<AppChangesDTO>((changesDto) =>
        //{
        //    var reply = CreateReplyMessageHeader(messageHeader, SparkMessageTypes.Ids.AppChangeNotification);
        //    var info = new AppChangeInfo()
        //    {
        //        ChannelName = changesDto.ChannelName,
        //        ReleaseName = changesDto.ReleaseName,
        //        AppEvent = (AppChangesEnum)changesDto.AppEvent,
        //        Message = changesDto.Message,
        //    };
        //    SendMessageInternal(reply, info);
        //});

        //SparkDisplayClient.SharedSparkClient.SubscribeToAppChanges();

        SendMessageInternal(messageHeader);
    }

    private void HandleUnsubscribeToAppChange(AhsokaMessageHeader messageHeader) 
    { 
        //SparkDisplayClient.SharedSparkClient.UnsubscribeToAppChanges();

        SendMessageInternal(messageHeader);
    }

    private void HandleUpdateLicenseFile(AhsokaMessageHeader messageHeader, ModuleLicenseInfo licenseInfo)
    {
        WriteLicenseFile(licenseInfo);

        SendMessageInternal(messageHeader);
    }

    private async void HandleGetRelease(AhsokaMessageHeader messageHeader, SparkReleaseDownloadRequest downloadRequest, CancellationTokenSource source)
    {
        try
        {
            SparkSubscriptionInfo info = GetSparkSubscription();

            var privateKey = SystemInfo.HardwareInfo.GetDeviceSigningKey(false);

            Console.WriteLine("Getting Auth Token");
            var auth = GetDeviceAuthorizationToken(info.SubscriptionUrl, Guid.Parse(info.DeviceId), privateKey);
            if (auth == null)
                throw new ApplicationException("No authorization found");
            Console.WriteLine("Received Auth -> Getting Containers");

            ReleaseDTO downloadToken;
            try
            {
                var releaseManifest = SparkDisplayClient.SharedSparkClient.GetReleaseManifest(downloadRequest.ReleaseId);
                downloadToken = SparkDisplayClient.SharedSparkClient.GetDownloadToken2(releaseManifest, DateTime.Now.AddHours(4));
                Console.WriteLine($"Received {downloadToken.ReleaseName} DownloadToken");
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.Message);
                throw new ApplicationException($"Release ID {downloadRequest.ReleaseId} is not found or current");
            }


            string urlToDownload = downloadToken.Contents.FirstOrDefault(x => x.Path.EndsWith(PackageType.opvEncrypted.GetFileExtension()) || x.Path.EndsWith("pvx1")).DownloadToken;
            string pathToFile = downloadRequest.PathToFile;
            if (urlToDownload == null) throw new ApplicationException("Package does not contain OpenPV install");

            var reply = CreateReplyMessageHeader(messageHeader, SparkMessageTypes.Ids.GetReleaseNotification);

            using var client = new DownloadUtility(urlToDownload, pathToFile);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                Console.WriteLine($"{progressPercentage}% ({totalBytesDownloaded}/{totalFileSize})");

                SparkReleaseDownloadNotification notification = new()
                {
                    PercentCompleted = (int)progressPercentage,
                    Status = MessageReplyStatus.Continue
                };

                SendMessageInternal(reply, notification);
            };

            await client.StartDownload(TimeSpan.FromSeconds(downloadRequest.TimeoutInSeconds), source);

            Console.WriteLine($"Download Complete");

            SparkReleaseDownloadCompleted downloadCompleteMessage = new()
            {
                ReleaseId = downloadRequest.ReleaseId,
                Status = MessageReplyStatus.Success
            };

            SendMessageInternal(messageHeader, downloadCompleteMessage);

        }
        catch (Exception ex)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.Message);

            SparkReleaseDownloadCompleted downloadError = new()
            {
                ReleaseId = downloadRequest.ReleaseId,
                Status = MessageReplyStatus.Error,
                ResultText = ex.Message
            };
            SendMessageInternal(messageHeader, downloadError);
        }
    }

    private void HandleGetSparkReleases(AhsokaMessageHeader messageHeader)
    {
        try
        {
            SparkSubscriptionInfo info = GetSparkSubscription();

            // If no Provision INformation
            if (String.IsNullOrEmpty(info.ChannelName)) 
            {
                var subInfo = AutoProvisionDeviceDTO();
                if (info == null)
                {
                    AhsokaLogging.LogMessage( AhsokaVerbosity.High, "Could not retrieve Auto-Provisioning Info.");

                    SendMessageInternal(messageHeader, new SparkReleaseInfo()
                    {
                        ResultText = "Could not Auto Provision Display",
                        Result = MessageReplyStatus.Error
                    });

                    return;
                }
                else
                {
                    // Get Updated Subscription Info
                    info = GetSparkSubscription();
                }

            }


            var privateKey = SystemInfo.HardwareInfo.GetDeviceSigningKey(false);

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Getting Auth Token");
            var auth = GetDeviceAuthorizationToken(info.SubscriptionUrl, Guid.Parse(info.DeviceId), privateKey);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Received Auth -> Getting Containers");

            var containers = SparkDisplayClient.SharedSparkClient.GetReleases();
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Received {containers.Count} Containers");

            SparkReleaseInfo results = new();

            foreach (var item in containers)
            {
                results.Releases.Add(new SparkRelease()
                {
                    ReleaseId = item.ReleaseID.ToString(),
                    ChannelName = item.ChannelName,
                    ContainerVersion = item.ContainerVersion,
                    Track = (SparkTrack)item.TrackType,
                    ReleaseName = item.ReleaseName,
                    ContainerName = item.ContainerName,
                });
            }

            results.Result = MessageReplyStatus.Success;

            SendMessageInternal(messageHeader, results);
        }
        catch (Exception ex)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.Message);

            SparkReleaseInfo results = new()
            {
                Result = MessageReplyStatus.Error,
                ResultText = "Could not retrieve results from Spark"
            };

            SendMessageInternal(messageHeader, results);
        }
    }

    private void HandleGetSparkSubscription(AhsokaMessageHeader messageHeader)
    {
        var subscription = GetSparkSubscription();

        SendMessageInternal(messageHeader, subscription);
    }

    private void HandleSetSparkSubscription(AhsokaMessageHeader messageHeader, SparkSubscription sparkSubscription)
    {
        WriteSubscriptionInfo(sparkSubscription);

        SendMessageInternal(messageHeader);

    }

    private SparkSubscription AutoProvisionDeviceDTO()
    {
        // Verify that the System is "Valid" an not tampered with.
        var factoryInfo = SystemInfo.HardwareInfo.FactoryInfo;
        if (!SystemInfo.ValidatePlatform())
            return null;

        // Fetch Hardware ID
        var coreInterop = SystemService.GetInterop();
        var hardwareID = coreInterop.GetHardwareSerialNumber();
        
        try
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Attemping to Auto Provision at address {autoProvisionAddress}");
            var results = SparkDisplayClient.SharedSparkClient.AutoProvisionDevice(autoProvisionAddress, new AutoProvisionDeviceDTO()
            {
                DeviceHardwareID = hardwareID,
                DeviceID = factoryInfo.DeviceId,
                DevicePublicKey = GetSigningKey(), // This Converts to Spark XML Keys
                PartNumber = factoryInfo.PartNumber,
                PlatformFamily = factoryInfo.PlatformFamily.ToString(),
                SerialNumber = factoryInfo.SerialNumber,
            });

            if (results != null)
            {
                // Create Subscription Info
                var subInfo = new SparkSubscription()
                {
                    ChannelName = SparkCustomerTools.CreateChannelName(results.SubscriptionInfo.ApplicationName, results.SubscriptionInfo.ApplicationId.ToString()),
                    ReleaseName = results.SubscriptionInfo.ApplicationName,
                    DeviceId = results.DeviceInfo.DeviceID.ToString(),
                    SubscriptionUrl = autoProvisionAddress,
                    IsFactorySubscrption = false,
                };

                // Write License Info
                WriteLicenseFile(results.ModuleLicenseInfo);

                // Write Subscription Info
                WriteSubscriptionInfo(subInfo);

                return subInfo;
            }

            return null;
        } 
        catch (Exception ex)
        {
            if (ex.InnerException is SparkException sparkEx)
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, sparkEx.Message);
            else
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.Message);

            return null; 
        }
                    
    }

    private static SparkSubscriptionInfo GetSparkSubscription()
    {
        string deviceKey = GetSigningKey();

        var sub = SystemInfo.HardwareInfo.TargetPathInfo.GetCloudPath(CloudPaths.SparkSubscriptionFile);

        if (File.Exists(sub))
        {
            string subFile = File.ReadAllText(sub);
            var subscription = JsonUtility.Deserialize<SparkSubscription>(subFile);

            return new SparkSubscriptionInfo()
            {
                DeviceId = SystemInfo.HardwareInfo.FactoryInfo.DeviceId.ToString(),
                ChannelName = subscription.ChannelName,
                ReleaseName = subscription.ReleaseName,
                SubscriptionUrl = subscription.SubscriptionUrl,
                DeviceKey = deviceKey,
                SerialNumber = SystemInfo.HardwareInfo.FactoryInfo.SerialNumber,
                PartNumber = SystemInfo.HardwareInfo.FactoryInfo.PartNumber,
            };
        }
        else
        {
            return new SparkSubscriptionInfo()
            {
                DeviceId = SystemInfo.HardwareInfo.FactoryInfo.DeviceId.ToString(),
                DeviceKey = deviceKey,
                SerialNumber = SystemInfo.HardwareInfo.FactoryInfo.SerialNumber,
                PartNumber = SystemInfo.HardwareInfo.FactoryInfo.PartNumber,
            };
        }
    }

    private static string GetSigningKey()
    {
        string deviceKey = deviceSigningKey;

        //  Since the Display uses PEM Keys, we must convert to XML
        if (deviceKey != null  && deviceKey.Contains("BEGIN RSA PUBLIC KEY"))
            deviceKey = RSAEncryption.ConvertPEMToPublicXML(deviceKey);
       
        return deviceKey;
    }

    private static void WriteSubscriptionInfo(SparkSubscription sparkSubscription)
    {
        string path = SystemInfo.HardwareInfo.TargetPathInfo.GetCloudPath(CloudPaths.SparkSubscriptionFile);

        if (sparkSubscription.IsFactorySubscrption)
            path = SystemInfo.HardwareInfo.TargetPathInfo.GetCloudPath(CloudPaths.FactorySubscriptionFile);

        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonUtility.Serialize(sparkSubscription));
    }

    private static void WriteLicenseFile(ModuleLicenseInfo licenseInfo)
    {
        Console.WriteLine("Decoding License File");
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, licenseInfo);

        Console.WriteLine("Backing up and Writing new License File");

        string fileName = SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.ModuleLicenseFile);
        if (File.Exists(fileName))
            File.Move(fileName, fileName + ".old." + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

        File.WriteAllBytes(fileName, stream.ToArray());
    }

    private AuthorizationDTO GetDeviceAuthorizationToken(string url, Guid deviceId, string privateKey)
    {
        // Generate Fingerprint
        string deviceFingerPrint = SecurityUtilities.GenerateFingerPrint(deviceId);

        // Generate a Fingerprint for our device
        var signature = SecurityUtilities.GetDeviceSignature(deviceFingerPrint, privateKey, false);

        // Return Token
        return SparkDisplayClient.SharedSparkClient.LoginToSpark(url, SparkEndPointType.ConnectedDisplay, SecurityGroopType.DeviceId, deviceFingerPrint, SecureLocalStrings.ToSecureString(signature), null);
    }
    #endregion
}
