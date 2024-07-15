using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ahsoka.System;

/// <summary>
/// Accesses the System Information for Clients / Services
/// </summary>
public static class SystemInfo
{
    #region Fields
    static List<LicenseInfo> licenseInfos;
    static readonly object syncRoot = new();
    static HardwareInfo hardwareInformation;

    static readonly PlatformFamily currentPlatform;

    #endregion

    #region Properties
    /// <summary>
    /// Enumerates the list of Open Source Licenses included in the System.
    /// </summary>
    public static List<LicenseInfo> LicenseInfos
    {
        get
        {
            lock (syncRoot)
            {
                licenseInfos ??= LoadLicenseInfo();

                return licenseInfos;
            }
        }
    }

    /// <summary>
    /// Returns the Hardware Information for the Current Platform.
    /// </summary>
    public static HardwareInfo HardwareInfo
    {
        get
        {
            lock (syncRoot)
                if (hardwareInformation == null)
                {
                    hardwareInformation = LoadHardwareInformation();
                    if (!Directory.Exists(hardwareInformation.TargetPathInfo.GetRootPaths(RootPaths.TempRoot)))
                        Directory.CreateDirectory(hardwareInformation.TargetPathInfo.GetRootPaths(RootPaths.TempRoot));

                    if (!Directory.Exists(hardwareInformation.TargetPathInfo.GetRootPaths(RootPaths.LogRoot)))
                        Directory.CreateDirectory(hardwareInformation.TargetPathInfo.GetRootPaths(RootPaths.LogRoot));

                    if (!Directory.Exists(hardwareInformation.TargetPathInfo.GetRootPaths(RootPaths.ServiceDataRoot)))
                        Directory.CreateDirectory(hardwareInformation.TargetPathInfo.GetRootPaths(RootPaths.ServiceDataRoot));
                }

            return hardwareInformation;
        }
    }

    /// <summary>
    /// Returns the Current Platform Family
    /// </summary>
    public static PlatformFamily CurrentPlatform => currentPlatform;
    #endregion

    #region Methods
    static SystemInfo()
    {
        lock (syncRoot)
            currentPlatform = DeterminePlatform();
    }

    /// <summary>
    /// Returns if the Hardware Information can be validated.
    /// </summary>
    /// <returns></returns>
    public static bool ValidatePlatform()
    {
        try
        {
            var manifestData = File.ReadAllText(SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareManifestFile));
            var sigData = File.ReadAllBytes(SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareManifestSignatureFile));

            // Create Manifest FileInfo
            FactoryManifest manifest = JsonUtility.Deserialize<FactoryManifest>(manifestData);

            // Signature is not included in 
            var signatureCheck = RSAEncryption.VerifyDataSignature(sigData, UTF8Encoding.UTF8.GetBytes(manifestData), Properties.Resources.UntrustedPrivateKeyPEM);
            if (!signatureCheck)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Failed Signature Check");
                return false;
            }

            foreach (var fileItem in manifest.FileInfo)
            {
                string filePath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareRoot), fileItem.Path);

                using FileStream fs = new(filePath, FileMode.Open);

                var result = RSAEncryption.GetChecksum(fs);

                if (result != fileItem.CheckSum)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Failed Checksum on {fileItem.Path} - {result}");
                    return false;
                }
            }
        }
        catch { return false; }

        return true;
    }

    private static HardwareInfo LoadHardwareInformation()
    {
        using var stopwatch = new AhsokaStopwatch("LoadHardwareInformation");

        HardwareInfo hardwareInfo = null;

        string configData = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Selecting Windows Configuration");
            string fileName = Environment.ExpandEnvironmentVariables("%USERPROFILE%/Ahsoka/factory/hardware/HardwareConfiguration.json");
            hardwareInfo = File.Exists(fileName) ?
                 JsonUtility.Deserialize<HardwareInfo>(File.ReadAllText(fileName)) :
                 HardwareInfo.GetHardwareInfo(PlatformFamily.Windows64, "Desktop");
        }
        if (OperatingSystem.IsMacOS())
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Selecting MacOS Configuration");
            string fileName = Environment.ExpandEnvironmentVariables("/usr/local/Ahsoka/factory/hardware/HardwareConfiguration.json");
            hardwareInfo = File.Exists(fileName) ?
                 JsonUtility.Deserialize<HardwareInfo>(File.ReadAllText(fileName)) :
                 HardwareInfo.GetHardwareInfo(PlatformFamily.MacosArm64, "Desktop");
        }
        else if (OperatingSystem.IsLinux() && !Environment.Is64BitProcess)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Selecting ST Configuration");

            var jsonText = File.ReadAllText("/usr/local/Ahsoka/factory/hardware/HardwareConfiguration.json");
            var info = JsonUtility.Deserialize<HardwareInfo>(jsonText);

            hardwareInfo = info ?? HardwareInfo.GetHardwareInfo(PlatformFamily.OpenViewLinux, "S50");
        }
        else if (OperatingSystem.IsLinux() && Environment.Is64BitProcess)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Selecting Ubuntu Configuration");
            string fileName = "/usr/local/Ahsoka/factory/hardware/HardwareConfiguration.json";
            hardwareInfo = File.Exists(fileName) ?
                 JsonUtility.Deserialize<HardwareInfo>(File.ReadAllText(fileName)) :
                 HardwareInfo.GetHardwareInfo(PlatformFamily.Ubuntu64, "Desktop");
        }

        stopwatch.ResetTiming("HardwareInfo Loaded");
        SetOtpValues(hardwareInfo);
        stopwatch.StopTiming("OTP Loaded");

        return hardwareInfo;
    }

    private static void SetOtpValues(HardwareInfo hardwareInfo)
    {
        var coreInterop = SystemService.GetInterop();

        Guid deviceId = FactoryInfo.CreateDeviceId(hardwareInfo.FactoryInfo.PlatformFamily, coreInterop.GetHardwareSerialNumber());

        // Validate the Hardware Info
        if (OperatingSystem.IsWindows())
            hardwareInfo.FactoryInfo.DeviceId = deviceId;
        else if (hardwareInfo.FactoryInfo.DeviceId != deviceId)
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Warning, Hardware Serial Number Mismatch {hardwareInfo.FactoryInfo.DeviceId} - {deviceId}");
    }

    private static PlatformFamily DeterminePlatform()
    {
        if (OperatingSystem.IsWindows())
            return PlatformFamily.Windows64;
        else if (OperatingSystem.IsLinux())
        {
            if (Environment.Is64BitProcess)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Determine Platform: " + RuntimeInformation.OSDescription);
                if (RuntimeInformation.OSDescription.Contains("Ubuntu"))
                    return PlatformFamily.Ubuntu64;
                else
                    return PlatformFamily.OpenViewLinuxPro;
            }
            else
                return PlatformFamily.OpenViewLinux;
        }
        else if (OperatingSystem.IsMacOS())
            return PlatformFamily.MacosArm64;

        return PlatformFamily.Any;
    }

    private static List<LicenseInfo> LoadLicenseInfo()
    {
        List<LicenseInfo> licenseInfo = new();
        foreach (var item in JsonUtility.Deserialize<List<LicenseInfo>>(Properties.Resources.Licenses))
            if (!(item.PackageName.StartsWith("Ahsoka.")))
                licenseInfo.Add(item);

        foreach (var item in JsonUtility.Deserialize<List<LicenseInfo>>(Properties.Resources.StaticLicenses))
            if (!(item.PackageName.StartsWith("Ahsoka.")))
                licenseInfo.Add(item);

        return licenseInfo;
    }

    #endregion
}

/// <summary>
/// Open Source License Information
/// </summary>
public class LicenseInfo
{
    /// <summary>
    /// Name of the Open Source Package
    /// </summary>
    public string PackageName { get; set; }

    /// <summary>
    /// Version of the Included Open Source Package
    /// </summary>
    public string PackageVersion { get; set; }

    /// <summary>
    /// URL to the Package License Information
    /// </summary>
    public string LicenseUrl { get; set; }

    /// <summary>
    /// Type of License used for the Open Source
    /// </summary>
    public string LicenseType { get; set; }

    /// <summary>
    /// Copyright Information if Specified by the Supplier / Project
    /// </summary>
    public string Copyright { get; set; }
}


public partial class RuntimeConfiguration
{
    /// <summary>
    /// 
    /// </summary>
    public StartOptions ServiceStartup { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public StartOptions ReleaseOption { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<ExtensionInfo> ExtensionInfo { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Int32 StartupDelay { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string ServiceKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public ChallengeOptions RequireChallenge { get; set; }
}


/// <summary>
/// Extension Information
/// </summary>
public partial class ExtensionInfo
{
    /// <summary>
    /// Extension Name
    /// </summary>
    public string ExtensionName { get; set; }

    /// <summary>
    /// Specific Version
    /// </summary>
    public string SpecificVersion { get; set; }
    
    /// <summary>
    /// Specific Version
    /// </summary>
    public string ConfigurationFile { get; set; }
}