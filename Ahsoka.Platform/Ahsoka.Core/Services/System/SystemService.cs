#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System.Platform;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.System;

[AhsokaService(Name)]
public class SystemService : AhsokaServiceBase<SystemMessageTypes.Ids>
{
    #region Fields
    public const string Name = "SystemService";

    bool isInstaller;
    static string deviceSigningKey;

    readonly IServiceImplementation coreInterop;
    readonly SystemSettings settings;
    readonly CancellationTokenSource cancelNotifications;
    Dictionary<string, string> platformVersions = new();
    static bool hardwareInteractionDisabled = false;

    public static bool HardwareInteractionDisabled { get => hardwareInteractionDisabled;  }
    private string currentAppPath;
    private string sdkVersion;
    private string applicationName;
    private string applicationDescription;
    private string applicationVersion;
    #endregion

    #region Events
    public event EventHandler<LoadApplicationCompleteEventArgs> AutoLoadReceived;
    #endregion

    #region Methods
    internal static IServiceImplementation GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WindowsServiceImplementation(),
            PlatformFamily.Ubuntu64 => new UbuntuServiceImplementation(),
            PlatformFamily.OpenViewLinux => new STServiceImplementation(),
            PlatformFamily.MacosArm64 => new MacOSServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    public SystemService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
  
    }

    public SystemService(ServiceConfiguration config) :
        base(config, new SystemServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create SystemService");

        stopwatch.ResetTiming("Interop Found");

        coreInterop = GetInterop();
        settings = LoadSystemSettings();
        InitSystemSettings(settings);
        stopwatch.ResetTiming("Settings Loaded");

        currentAppPath = GetSystemInfo(out applicationName, out applicationDescription, out applicationVersion, out sdkVersion);

        GetVersions();

        stopwatch.ResetTiming("Hardware Info Loaded");

        InitValues();

        // If Hardware Enabled Start Ignition Thread
        if (!HardwareInteractionDisabled)
        {
            cancelNotifications = new CancellationTokenSource();

            UpdateCacheValue(SystemServiceDataKeys.CurrentIgnitionState, IgnitionStates.Unknown.EnumToInt());

            Task.Factory.StartNew(() =>
            {
                try
                {
                    IgnitionStates ignitionState = IgnitionStates.Unknown;  // Assume on at start-up
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Ignition Monitor Started");

                    while (!cancelNotifications.IsCancellationRequested)
                    {
                        var response = coreInterop.GetIGNPin();
                        if (ignitionState != response.State && response.State != IgnitionStates.Unknown)
                        {
                            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Ignition Monitor: IgnitionState is {response.State}");
                            ignitionState = response.State;

                            int transportID = SystemMessageTypes.Ids.IgnitionOnNotification.EnumToInt();
                            if (ignitionState == IgnitionStates.Off)
                                transportID = SystemMessageTypes.Ids.IgnitionOffNotification.EnumToInt();

                            var header = new AhsokaMessageHeader()
                            {
                                TransportId = transportID
                            };

                            // Update Ignition in Data Service
                            UpdateCacheValue(SystemServiceDataKeys.CurrentIgnitionState, ignitionState.EnumToInt());

                            SendMessageInternal(header, new EmptyNotification(), true);
                        }

                        Task.Delay(1000, cancelNotifications.Token).Wait();
                    }
                }
                catch (Exception ex)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Ignition notifications terminated with exception: {ex.Message}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.StackTrace);
                }
            });

            stopwatch.StopTiming("Ignition Thread Started");
        }
    }

    private void InitValues()
    {
       
        isInstaller = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIExecutablePath).EndsWith(applicationName);

        // Check if Run Path is under App Directory
        if (Environment.ProcessPath.ToLower().StartsWith(currentAppPath.ToLower()))
        {
            string appTypeFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationType);
            if (File.Exists(appTypeFile) && File.ReadAllText(appTypeFile) == ApplicationType.CODESYS.ToString())
            {
                hardwareInteractionDisabled = true;
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Hardware Interaction is Disabled for CODESYS Targets");
            }
        }

        UpdateCacheValue(SystemServiceDataKeys.Available, true);
        UpdateCacheValue(SystemServiceDataKeys.CurrentApplicationName, applicationName);
    }

    internal static string GetSystemInfo(out string applicationName, out string applicationDescription, out string applicationVersion, out string sdkVersion )
    {
        applicationName = Path.GetFileName(Environment.ProcessPath).Replace(".exe", ""); // remove exe from windows exe's
        var currentAppPath = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);
        sdkVersion = VersionUtility.GetAppVersionString();
        applicationVersion = applicationDescription = string.Empty;

        if (Environment.ProcessPath.ToLower().StartsWith(currentAppPath.ToLower()))
        {
            string appNameFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationName);
            if (File.Exists(appNameFile))
                applicationName = File.ReadAllText(appNameFile);

            string applicationVersionPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationVersion);
            if (File.Exists(applicationVersionPath))
                applicationVersion = File.ReadAllText(applicationVersionPath);

            string applicationDescriptionPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationDescription);
            if (File.Exists(applicationDescriptionPath))
                applicationDescription = File.ReadAllText(applicationDescriptionPath);

        }

        return currentAppPath;
    }

    private void GetVersions()
    {
        platformVersions = coreInterop.GetPlatformVersions();

        if (applicationVersion != null)
            platformVersions[SystemServiceDataKeys.ApplicationVersion] = applicationVersion;

        if (applicationDescription != null)
            platformVersions[SystemServiceDataKeys.ApplicationDescription] = applicationDescription;

        platformVersions[SystemServiceDataKeys.SDKVersion] = sdkVersion;

        string installerPathFactory = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerVersion);
        string installerPathField = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerVersion, false);

        // Set InstallerVersion to Field version if exists, Factory version if does not exist
        if (File.Exists(installerPathField))
            platformVersions[SystemServiceDataKeys.InstallerVersion] = File.ReadAllText(installerPathField);
        else if (File.Exists(installerPathFactory))
            platformVersions[SystemServiceDataKeys.InstallerVersion] = File.ReadAllText(installerPathFactory);

        // Always set InstallerVersionFactory to Factory version
        if (File.Exists(installerPathFactory))
            platformVersions[SystemServiceDataKeys.InstallerVersionFactory] = File.ReadAllText(installerPathFactory);

        // Add Keys to Data Service
        foreach(var item in platformVersions)
            UpdateCacheValue(item.Key, item.Value);
    }

    ~SystemService()
    {
        cancelNotifications?.Cancel();
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<SystemMessageTypes.Ids>())
        {
            case SystemMessageTypes.Ids.Echo:

                HandleEchoValue(messageHeader, message);
                break;

            case SystemMessageTypes.Ids.GetServiceInfo:

                HandleGetServiceInfo(messageHeader);
                break;

            case SystemMessageTypes.Ids.RequestShutdown:

                HandleShutdown(messageHeader, message as ShutdownInformation);
                break;

            case SystemMessageTypes.Ids.GetHardwareInformation:

                HandleGetHardwareInformation(messageHeader);
                break;

            case SystemMessageTypes.Ids.LoadApplication:
            case SystemMessageTypes.Ids.LoadApplicationCancelled:
            case SystemMessageTypes.Ids.LoadApplicationData:

                HandleLoadApplicationMessage(messageHeader, message);
                break;

            case SystemMessageTypes.Ids.GetBrightness:
                HandleGetBrightness(messageHeader);
                break;

            case SystemMessageTypes.Ids.SetBrightness:
                HandleSetBrightness(messageHeader, message as BrightnessInfo);
                break;

            case SystemMessageTypes.Ids.GetBuzzerConfig:
                HandleGetBuzzerConfig(messageHeader);
                break;

            case SystemMessageTypes.Ids.SetBuzzerConfig:
                HandleSetBuzzerConfig(messageHeader, message as BuzzerConfig);
                break;

            case SystemMessageTypes.Ids.GetUsbInfo:
                HandleGetUsbInfo(messageHeader);
                break;

            case SystemMessageTypes.Ids.SetUsbInfo:
                HandleSetUsbInfo(messageHeader, message as UsbInfo);
                break;

            case SystemMessageTypes.Ids.SetSerialConfig:
                HandleSetRS485Config(messageHeader, (RS485ConfigNotification)message);
                break;

            case SystemMessageTypes.Ids.GetBatteryVoltage:
                HandleGetVBat(messageHeader);
                break;

            case SystemMessageTypes.Ids.GetIgnitionPin:
                HandleGetIGNPin(messageHeader);
                break;

            case SystemMessageTypes.Ids.UpdateTouchTune:
                Console.WriteLine("SystemService: OnHandleReceive: UpdateTouchscreen");
                HandleUpdateTouchscreen(messageHeader, message as TouchTuneInfo);
                break;

            case SystemMessageTypes.Ids.SetBootLogo:
                HandleSetBootLogo(messageHeader, message as BootLogoInfo);
                break;
        }
    }

    private void HandleShutdown(AhsokaMessageHeader messageHeader, ShutdownInformation message)
    {
        SendShutdownNotification(messageHeader);

        if (message.ShutdownType == ShutdownTypes.ShutdownServices)
        {
            // Shutting Down From the Work Thread may Block
            Task.Run(() => { CurrentRuntime.RequestShutdown(); });
        }
        else if (message.ShutdownType == ShutdownTypes.ShutdownApplication)
        {
            coreInterop.ShutdownApplication(message.ShutdownCode);
        }
        else if (message.ShutdownType == ShutdownTypes.Reboot)
        {
            coreInterop.Reboot();
        }
    }

    private void HandleLoadApplicationMessage(AhsokaMessageHeader messageHeader, object message)
    {
        if (message is LoadApplication loadApplication) // Replies with LoadAppReply
        {
            var helper = FileTransferServiceHelper.CreateFileTransferHelper(loadApplication);
            if (helper != null)
            {
                var response = helper.GetLoadReply<LoadApplicationReply>();
                SendMessageInternal(messageHeader, response);
                Debug.WriteLine($"File Upload Started {loadApplication.FileName}");
            }
            else
            {
                var response = new LoadApplicationReply()
                {
                    ReplyStatus = MessageReplyStatus.Error,
                    ReplyReason = $"Upload for file {loadApplication.FileName} is already in progress."
                };

                SendMessageInternal(messageHeader, response);
                Debug.WriteLine($"File Upload Rejected {loadApplication.FileName}");
            }

        }
        else if (message is LoadApplicationData loadData) // Replys with LoadDataReply.
        {
            if (FileTransferServiceHelper.TryGetFileTransferHelper(loadData.UploadId, out var transferHelper))
            {
                // If transfer helper has a response (it my not) then return it.
                var result = transferHelper.HandleData<LoadApplicationDataReply>(loadData);
                if (result != null)
                {
                    SendMessageInternal(messageHeader, result);

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
                SendMessageInternal(messageHeader, new LoadApplicationDataReply()
                {
                    UploadId = loadData.UploadId,
                    ReplyStatus = MessageReplyStatus.Error,
                    ReplyReason = "No Transfer Found in Progress for Id"
                });
            }
        }
        else if (message is LoadApplicationCancelled clientCancelled) // Reply with Load Data Reply
        {
            if (FileTransferServiceHelper.TryGetFileTransferHelper(clientCancelled.UploadId, out var transferHelper))
            {
                var result = transferHelper.HandleClientCancellation<LoadApplicationDataReply>(clientCancelled);

                SendMessageInternal(messageHeader, result);
            }
        }
    }

    private void HandleGetHardwareInformation(AhsokaMessageHeader messageHeader)
    {
        if (deviceSigningKey == null)
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
            response.VersionInfoes[item.Key] = item.Value;

        SendMessageInternal(messageHeader, response);
    }

    public void SendShutdownNotification(AhsokaMessageHeader messageHeader)
    {
        SendMessageInternal(messageHeader);
    }

    private void HandleGetServiceInfo(AhsokaMessageHeader messageHeader)
    {
        var response = new ServiceInfo();
        foreach (var item in GetServiceInfo())
            response.ServiceStats.Add(item);

        SendMessageInternal(messageHeader, response);
    }

    private void HandleEchoValue(AhsokaMessageHeader messageHeader, object message)
    {
        object returnObject = new EchoValue() { DataValue = (message as EchoValue).DataValue + " From Server" };

        SendMessageInternal(messageHeader, returnObject);
    }

    private void HandleGetBrightness(AhsokaMessageHeader messageHeader)
    {
        SendMessageInternal(messageHeader, settings.BrightnessInfo);
    }

    private void HandleSetBrightness(AhsokaMessageHeader messageHeader, BrightnessInfo brightnessInfo)
    {
        coreInterop.SetBrightness(brightnessInfo.Percentage);
        settings.BrightnessInfo.Percentage = brightnessInfo.Percentage;
        if (brightnessInfo.Persist)
            SaveSystemSettings();
        HandleGetBrightness(messageHeader);
    }

    private void HandleGetBuzzerConfig(AhsokaMessageHeader messageHeader)
    {
        SendMessageInternal(messageHeader, coreInterop.GetBuzzerConfig());
    }

    private void HandleSetBuzzerConfig(AhsokaMessageHeader messageHeader, BuzzerConfig buzzerConfig)
    {
        coreInterop.SetBuzzerConfig(buzzerConfig);
        SendMessageInternal(messageHeader);
    }

    private void HandleGetUsbInfo(AhsokaMessageHeader messageHeader)
    {
        SendMessageInternal(messageHeader, coreInterop.GetUsbInfo());
    }

    private void HandleSetUsbInfo(AhsokaMessageHeader messageHeader, UsbInfo usbInfo)
    {
        coreInterop.SetUsbInfo(usbInfo);
        SendMessageInternal(messageHeader);
    }

    private void HandleSetRS485Config(AhsokaMessageHeader messageHeader, RS485ConfigNotification serial_config)
    {
        // We are converting the protobuf structure back to it's base type to set the config locally.
        var serial_rs485_config = new Platform.SerialRS485()
        {
            flags = serial_config.Flags,
            delay_rts_after_send = serial_config.DelayRtsAfterSend,
            delay_rts_before_send = serial_config.DelayRtsBeforeSend
        };
        coreInterop.SetRS485Config(serial_rs485_config);

        SendMessageInternal(messageHeader);
    }

    internal List<ServiceStats> GetServiceInfo()
    {
        List<ServiceStats> services = new();
        foreach (var service in CurrentRuntime.EndPoints)
        {
            IAhsokaServiceEndpoint endPoint = service.Value as IAhsokaServiceEndpoint;

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
            SystemSettings systemSettings = JsonUtility.Deserialize<SystemSettings>(File.ReadAllText(file));
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

    private void HandleGetVBat(AhsokaMessageHeader messageHeader)
    {
        if (HardwareInteractionDisabled)
        {
            SendMessageInternal(messageHeader, new VoltageValue());
            return;
        }

        var response = coreInterop.GetVBat();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleGetIGNPin(AhsokaMessageHeader messageHeader)
    {
        if (HardwareInteractionDisabled)
        {
            SendMessageInternal(messageHeader, new IgnitionState() { State = IgnitionStates.Unknown });
            return;
        }

        var response = coreInterop.GetIGNPin();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleUpdateTouchscreen(AhsokaMessageHeader messageHeader, TouchTuneInfo updateTouchscreen)
    {
        var response = coreInterop.UpdateTouchTune(updateTouchscreen);
        SendMessageInternal(messageHeader, response);
    }

    private void HandleSetBootLogo(AhsokaMessageHeader messageHeader, BootLogoInfo bootLogoInfo)
    {
        coreInterop.SetBootLogo(bootLogoInfo.Path);
        SendMessageInternal(messageHeader);
    }

    #endregion
}

public class SystemSettings
{
    public BrightnessInfo BrightnessInfo { get; set; }
}

public partial class BrightnessInfo
{
    public BrightnessInfo()
    {
        this.Persist = true;
    }
}