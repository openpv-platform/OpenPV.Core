using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Installer.Components;
using Ahsoka.Services.System.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.System;

/// <summary>
/// System Service manages System Wide features such as Loading Applications, Getting Hardware Information etc.
/// </summary>
[AhsokaService(Name)]
public class SystemService : AhsokaServiceBase<SystemMessageTypes.Ids>
{
    #region Fields
    internal const string Name = "SystemService";

    bool isInstaller;
    static string deviceSigningKey;

    readonly IServiceImplementation coreInterop;
    readonly SystemSettings settings;

    Dictionary<string, string> platformVersions = new();
    private string currentAppPath;
    private string sdkVersion;
    private string applicationName;
    private string applicationDescription;
    private string applicationVersion;
    #endregion

    #region Events
    /// <summary>
    /// Event Handler to notify Installers that a new File has Arrived to be Installed
    /// </summary>
    public event EventHandler<LoadApplicationCompleteEventArgs> AutoLoadReceived;
    #endregion

    #region Methods
    internal static IServiceImplementation GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new DesktopServiceImplementation(),
            PlatformFamily.Ubuntu64 => new DesktopServiceImplementation(),
            PlatformFamily.OpenViewLinux => new STServiceImplementation(),
            PlatformFamily.MacOSArm64 => new DesktopServiceImplementation(),
            PlatformFamily.OpenViewLinuxPro => new TIServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    /// <summary>
    /// Default CTOR
    /// </summary>
    public SystemService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    /// <summary>
    /// Constructor for Overriding Configuration
    /// </summary>
    /// <param name="config"></param>
    public SystemService(ServiceConfiguration config) :
        base(config, new SystemServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create SystemService");

        stopwatch.ResetTiming("Interop Found");

        coreInterop = GetInterop();
        settings = LoadSystemSettings();
        InitSystemSettings(settings);
        stopwatch.ResetTiming("Settings Loaded");

        currentAppPath = SystemInfo.GetApplicationInfo(out applicationName, out applicationDescription, out applicationVersion, out sdkVersion);

        GetVersions();

        stopwatch.ResetTiming("Hardware Info Loaded");

        InitValues();

    }

    private void InitValues()
    {
        isInstaller = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIExecutablePath).EndsWith(applicationName);

        UpdateCacheValue(SystemServiceMessages.Available, true);
        UpdateCacheValue(SystemServiceMessages.CurrentApplicationName, applicationName);
    }

    private void GetVersions()
    {
        platformVersions = coreInterop.GetPlatformVersions();

        if (applicationVersion != null)
            platformVersions[SystemServiceMessages.ApplicationVersion] = applicationVersion;

        if (applicationDescription != null)
            platformVersions[SystemServiceMessages.ApplicationDescription] = applicationDescription;

        platformVersions[SystemServiceMessages.SDKVersion] = sdkVersion;

        string installerPathFactory = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerVersion);
        string installerPathField = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerVersion, false);

        // Set InstallerVersion to Field version if exists, Factory version if does not exist
        if (File.Exists(installerPathField))
            platformVersions[SystemServiceMessages.InstallerVersion] = File.ReadAllText(installerPathField);
        else if (File.Exists(installerPathFactory))
            platformVersions[SystemServiceMessages.InstallerVersion] = File.ReadAllText(installerPathFactory);

        // Always set InstallerVersionFactory to Factory version
        if (File.Exists(installerPathFactory))
            platformVersions[SystemServiceMessages.InstallerVersionFactory] = File.ReadAllText(installerPathFactory);

        // Add Keys to Data Service
        foreach (var item in platformVersions)
            UpdateCacheValue(item.Key, item.Value);
    }

    /// <summary>
    /// Service Handler Method - Raised when Service Messages Arrive
    /// </summary>
    /// <param name="request"></param>
    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case SystemMessageTypes.Ids.Echo:

                HandleEchoValue(request);
                break;

            case SystemMessageTypes.Ids.GetServiceInfo:

                HandleGetServiceInfo(request);
                break;

            case SystemMessageTypes.Ids.RequestShutdown:

                HandleShutdown(request);
                break;

            case SystemMessageTypes.Ids.GetHardwareInformation:

                HandleGetHardwareInformation(request);
                break;

            case SystemMessageTypes.Ids.LoadApplication:
            case SystemMessageTypes.Ids.LoadApplicationCancelled:
            case SystemMessageTypes.Ids.LoadApplicationData:

                HandleLoadApplicationMessage(request);
                break;

            case SystemMessageTypes.Ids.GetBrightness:
                HandleGetBrightness(request);
                break;

            case SystemMessageTypes.Ids.SetBrightness:
                HandleSetBrightness(request);
                break;

            case SystemMessageTypes.Ids.SetBootLogo:
                HandleSetBootLogo(request);
                break;

            case SystemMessageTypes.Ids.RequestNotif:
                HandleNotif(request);
                break;
        }
    }

    private void HandleNotif(AhsokaServiceRequest message)
    {
        SendResponse(message, message.Message);

        SendNotification(SystemMessageTypes.Ids.TestNotif, message.Message, message.Header.ClientId); // Send ONLY to Registered Client
    }

    private void HandleShutdown(AhsokaServiceRequest message)
    {
        ShutdownInformation shutdownInfo = message.Message as ShutdownInformation;
        SendResponse(message, shutdownInfo);

        if (shutdownInfo.ShutdownType == ShutdownTypes.ShutdownServices)
        {
            // Shutting Down From the Work Thread may Block
            Task.Run(() => { AhsokaRuntime.Default.StopAllEndPoints(false, true); });
        }
        else if (shutdownInfo.ShutdownType == ShutdownTypes.ShutdownApplication)
        {
            coreInterop.ShutdownApplication(shutdownInfo.ShutdownCode);
        }
        else if (shutdownInfo.ShutdownType == ShutdownTypes.Reboot)
        {
            coreInterop.Reboot();
        }
    }

    private void HandleLoadApplicationMessage(AhsokaServiceRequest message)
    {
        if (message.Message is LoadApplication loadApplication) // Replies with LoadAppReply
        {
            var helper = FileTransferServiceHelper.CreateFileTransferHelper(loadApplication);
            if (helper != null)
            {
                var response = helper.GetLoadReply<LoadApplicationReply>();
                SendResponse(message, response);
                Debug.WriteLine($"File Upload Started {loadApplication.FileName}");
            }
            else
            {
                var response = new LoadApplicationReply()
                {
                    ReplyStatus = MessageReplyStatus.Error,
                    ReplyReason = $"Upload for file {loadApplication.FileName} is already in progress."
                };

                SendResponse(message, response);
                Debug.WriteLine($"File Upload Rejected {loadApplication.FileName}");
            }

        }
        else if (message.Message is LoadApplicationData loadData) // Replys with LoadDataReply.
        {
            if (FileTransferServiceHelper.TryGetFileTransferHelper(loadData.UploadId, out var transferHelper))
            {
                // If transfer helper has a response (it my not) then return it.
                var result = transferHelper.HandleData<LoadApplicationDataReply>(loadData);
                if (result != null)
                {
                    SendResponse(message, result);

                    if (result.ReplyStatus == MessageReplyStatus.Success)
                    {
                        LoadApplication loadInfo = transferHelper.TransmitInfo as LoadApplication;

                        string filePath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder), loadInfo.FileName);
                        AutoLoadReceived?.Invoke(this, new LoadApplicationCompleteEventArgs() { FileName = filePath, Autoload = loadInfo.Autoload });
                    }
                }
            }
            else // No Transfer Object Found
            {
                SendResponse(message, new LoadApplicationDataReply()
                {
                    UploadId = loadData.UploadId,
                    ReplyStatus = MessageReplyStatus.Error,
                    ReplyReason = "No Transfer Found in Progress for Id"
                });
            }
        }
        else if (message.Message is LoadApplicationCancelled clientCancelled) // Reply with Load Data Reply
        {
            if (FileTransferServiceHelper.TryGetFileTransferHelper(clientCancelled.UploadId, out var transferHelper))
            {
                var result = transferHelper.HandleClientCancellation<LoadApplicationDataReply>(clientCancelled);

                SendResponse(message, result);
            }
        }
    }

    private void HandleGetHardwareInformation(AhsokaServiceRequest message)
    {
        if (deviceSigningKey == null && !SystemInfo.IsDesktop())
            deviceSigningKey = SystemInfo.HardwareInfo.GetDeviceSigningKey();

        var response = new GetHardwareInformationReply()
        {
            SerialNumber = SystemInfo.HardwareInfo.FactoryInfo.SerialNumber,
            PartNumber = SystemInfo.HardwareInfo.FactoryInfo.PartNumber,
            DeviceId = SystemInfo.HardwareInfo.FactoryInfo.DeviceId.ToString(),
            PlatformFamily = SystemInfo.HardwareInfo.FactoryInfo.PlatformFamily,
            PlatformQualifier = SystemInfo.HardwareInfo.FactoryInfo.PlatformQualifier,
            ApplicationName = applicationName,
            DeviceKey = deviceSigningKey,
            IsInstaller = isInstaller,
            CpuTempInC = coreInterop.GetCPUTemp()
        };

        foreach (var item in platformVersions)
            response.VersionInfo[item.Key] = item.Value;

        SendResponse(message, response);
    }

    private void HandleGetServiceInfo(AhsokaServiceRequest message)
    {
        var response = new ServiceInfo();
        foreach (var item in GetServiceInfo())
            response.ServiceStats.Add(item);

        SendResponse(message, response);
    }

    private void HandleEchoValue(AhsokaServiceRequest message)
    {
        object returnObject = new EchoValue()
        {
            DataValue = (message.Message as EchoValue).DataValue + " From Server"
        };

        SendResponse(message, returnObject);
    }

    private void HandleGetBrightness(AhsokaServiceRequest message)
    {
        SendResponse(message, settings.BrightnessInfo);
    }

    private void HandleSetBrightness(AhsokaServiceRequest message)
    {
        var info = message.Message as BrightnessInfo;
        coreInterop.SetBrightness(info.Percentage);
        settings.BrightnessInfo.Percentage = info.Percentage;
        if (info.Persist)
            SaveSystemSettings();

        HandleGetBrightness(message);
    }

    internal List<ServiceStats> GetServiceInfo()
    {
        List<ServiceStats> services = new();
        foreach (var service in AhsokaRuntime.Default.ServiceEndPoints)
        {
            IAhsokaServiceEndPoint endPoint = service as IAhsokaServiceEndPoint;

            services.Add(new ServiceStats()
            {
                ServiceAddress = endPoint.ServiceConfig.DataURI,
                ServiceName = endPoint.ServiceConfig.ServiceName,
                ReceiveMessageCount = endPoint.MessageReceiveCount,
                SendMessageCount = endPoint.MessageSendCount
            });
        }

        return services;
    }

    private SystemSettings LoadSystemSettings()
    {
        string file = GetSettingsPath();
        if (File.Exists(file))
        {
            SystemSettings systemSettings = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(file), CoreJsonSerializerContext.Default.SystemSettings);
            if (systemSettings == null)
            {
                Console.WriteLine($"LoadSystemSettings: systemSettings is null...");
                return new SystemSettings()
                {
                    BrightnessInfo = new BrightnessInfo()
                    {
                        Percentage = coreInterop.GetBrightness()
                    }
                };
            }
            else if (systemSettings.BrightnessInfo == null)
            {
                Console.WriteLine($"LoadSystemSettings: systemSettings.BrightnessInfo is null...");
                // Deserialized reference type is null, file is likely empty
                systemSettings.BrightnessInfo = new BrightnessInfo()
                {
                    Percentage = coreInterop.GetBrightness()
                };
                return systemSettings;
            }
            else
            {
                return systemSettings;
            }
        }

        return new SystemSettings()
        {
            BrightnessInfo = new BrightnessInfo()
            {
                Percentage = coreInterop.GetBrightness()
            }
        };
    }

    private void SaveSystemSettings()
    {
        string file = GetSettingsPath();
        File.WriteAllText(file, JsonUtility.Serialize(settings));
    }

    private void InitSystemSettings(SystemSettings systemSettings)
    {
        coreInterop.SetBrightness(systemSettings.BrightnessInfo.Percentage);
    }

    private static string GetSettingsPath()
    {
        string path = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ServiceDataRoot), "system");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        string file = Path.Combine(path, "settings.json");
        return file;
    }

    private void HandleSetBootLogo(AhsokaServiceRequest message)
    {
        var bootLogoInfo = message.Message as BootLogoInfo;
        string tempFile = Path.GetTempFileName() + Path.GetExtension(bootLogoInfo.Path);
        if (bootLogoInfo.Rotation != BootLogoRotation.NoRotation)
        {
            if (File.Exists(bootLogoInfo.Path))
            {
                using var ms = new MemoryStream(File.ReadAllBytes(bootLogoInfo.Path));

                var data = ImageUtility.RotateImage((int)bootLogoInfo.Rotation, ms);
                File.WriteAllBytes(tempFile, data.ToArray());
                bootLogoInfo.Path = tempFile;
            }
        }

        coreInterop.SetBootLogo(bootLogoInfo.Path);
        SendResponse(message);

        if (File.Exists(tempFile))
            File.Delete(tempFile);
    }

    #endregion
}

internal class SystemSettings
{
    public BrightnessInfo BrightnessInfo { get; set; }
}

