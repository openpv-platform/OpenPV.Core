using Ahsoka.Installer.Components;
using Ahsoka.Installer.InstallEngine;
using System;
using System.IO;

namespace Ahsoka.System.Hardware;

/// <summary>
/// Object used to get paths used on the target based on the 
/// current hardware platform.
/// </summary>
public partial class TargetPathInfo
{
    private readonly HardwareInfo hardwareInformation;

    internal TargetPathInfo(HardwareInfo hardwareInfo)
    {
        this.hardwareInformation = hardwareInfo;
    }


    /// <summary>
    /// Get Paths for the Primary Folders in the Ahsoka System
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string GetRootPaths(RootPaths path)
    {
        return path switch
        {
            RootPaths.CloudRoot => GetAhsokaPath("cloud"),
            RootPaths.ApplicationRoot => GetAhsokaPath("current"),
            RootPaths.BackupRoot => GetAhsokaPath("backup"),
            RootPaths.DependencyRoot => GetAhsokaPath("dependency"),
            RootPaths.TempRoot => GetAhsokaPath("tmp"),
            RootPaths.FactoryRoot => GetAhsokaPath("factory"),
            RootPaths.FieldRoot => GetAhsokaPath("field"),
            RootPaths.LogRoot => GetAhsokaPath("log"),
            RootPaths.ServiceDataRoot => GetAhsokaPath("service"),
            _ => GetAhsokaPath("unknown"),
        };
    }

    /// <summary>
    /// Gets Paths used by the Installers
    /// </summary>
    /// <param name="path"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public string GetInstallerPath(InstallerPaths path, bool factory = true)
    {
        string factoryRoot = GetRootPaths(RootPaths.FactoryRoot);
        string fieldRoot = GetRootPaths(RootPaths.FieldRoot);

        switch (path)
        {
            case InstallerPaths.ApplicationSigningKeyPath:
                return $"{fieldRoot}/signing_key.cert";

            case InstallerPaths.WatchFolder:
                return $"{GetAhsokaPath("watcher")}";

            case InstallerPaths.InstallerUIRoot:
                if (factory)
                    return $"{factoryRoot}/installerui";
                else
                    return $"{fieldRoot}/installerui";

            case InstallerPaths.InstallerUIExecutablePath:
                if (factory)
                    return $"{factoryRoot}/installerui/Ahsoka.Installer.UI";
                else
                    return $"{fieldRoot}/installerui/Ahsoka.Installer.UI";

            case InstallerPaths.CommandLineRoot:
                return $"{factoryRoot}/commandline";

            case InstallerPaths.CommandLineExecutablePath:
                return $"{factoryRoot}/commandline/Ahsoka.CommandLine";

            case InstallerPaths.BootLogoFileName:
                if (OperatingSystem.IsWindows())
                    return $"{GetAhsokaPath("logo")}/splash.png";
                return $"/usr/share/psplash/splash.png";

            case InstallerPaths.ApplicationVersion:
                return Path.Combine(GetRootPaths(RootPaths.ApplicationRoot), ApplicationOutputComponent.AppInfoFolder , ApplicationOutputComponent.AppVersionFile);

            case InstallerPaths.ApplicationDescription:
                return Path.Combine(GetRootPaths(RootPaths.ApplicationRoot), ApplicationOutputComponent.AppInfoFolder , ApplicationOutputComponent.AppDescriptionFile);

            case InstallerPaths.ApplicationName:
                return Path.Combine(GetRootPaths(RootPaths.ApplicationRoot), ApplicationOutputComponent.AppInfoFolder, ApplicationOutputComponent.AppNameFile);

            case InstallerPaths.ApplicationType:
                return Path.Combine(GetRootPaths(RootPaths.ApplicationRoot), ApplicationOutputComponent.AppInfoFolder, ApplicationOutputComponent.AppTypeFile);

            case InstallerPaths.StartConfig:
                return Path.Combine(GetRootPaths(RootPaths.ApplicationRoot), ApplicationOutputComponent.AppInfoFolder, StartupInstallerPlugin.AutoStartFileName);

            case InstallerPaths.CoProcessorApplicationPath:
                return Path.Combine(GetRootPaths(RootPaths.ApplicationRoot), ApplicationOutputComponent.CoProcessorFolder);

            case InstallerPaths.InstallerVersion:
                if (factory)
                    return $"{factoryRoot}/installerui/InstallerVersion.txt";
                else
                    return $"{fieldRoot}/installerui/InstallerVersion.txt";
        }

        return factoryRoot;
    }

    /// <summary>
    /// Get Paths used by the Cloud / Spark Infrastructure.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string GetCloudPath(CloudPaths path)
    {
        string cloudPath = GetRootPaths(RootPaths.CloudRoot);

        switch (path)
        {
            case CloudPaths.SparkSubscriptionFile:
                return $"{cloudPath}/SparkSubscription.json"; ;
            case CloudPaths.FactorySubscriptionFile:
                return $"{cloudPath}/SparkFactorySubscription.json"; ;
        }

        return cloudPath;
    }

    /// <summary>
    /// Get Paths for accessing Factory Data.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string GetFactoryPath(FactoryPaths path)
    {
        string factoryRoot = GetRootPaths(RootPaths.FactoryRoot);
        string factoryHardwareRoot = $"{factoryRoot}/hardware";

        return path switch
        {
            FactoryPaths.HardwareRoot => factoryHardwareRoot,
            FactoryPaths.HardwareInformationFile => $"{factoryHardwareRoot}/HardwareConfiguration.json",
            FactoryPaths.HardwareDeviceSigningKeyFile => $"{factoryHardwareRoot}/DeviceKey.cert",
            FactoryPaths.HardwareDeviceSigningKeyPEMFile => $"{factoryHardwareRoot}/DeviceKeyPEM.cert",
            FactoryPaths.HardwareDeviceSigningPublicKeyFile => $"{factoryHardwareRoot}/DevicePublicKey.cert",
            FactoryPaths.HardwareDeviceSigningPublicKeyPEMFile => $"{factoryHardwareRoot}/DevicePublicKeyPEM.cert",
            FactoryPaths.HardwareManifestFile => $"{factoryHardwareRoot}/Manifest.info",
            FactoryPaths.HardwareManifestSignatureFile => $"{factoryHardwareRoot}/Manifest.sig",
            FactoryPaths.ModuleLicenseFile => $"{factoryHardwareRoot}/ModuleLicenseInfo.pbuff",
            _ => factoryRoot,
        };
    }

    /// <summary>
    /// Get the paths used for storing Service Data which 
    /// will persist across a reboot.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string GetServiceDataPath(ServiceDataPaths path)
    {
        string serviceDataRoot = $"{GetAhsokaPath("service")}";

        return path switch
        {
            ServiceDataPaths.NetworkProfileFolder => $"{serviceDataRoot}/network/profiles",
            _ => serviceDataRoot,
        };
    }

    /// <summary>
    /// Get the paths used for storing Service Data which 
    /// will persist across a reboot.
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public string GetServiceDataPath(string serviceName)
    {
        string serviceDataRoot = $"{GetAhsokaPath("service")}";
        string folder = $"{serviceDataRoot}/{serviceName}";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>
    /// Create a Temp File for use in writing scratch / temp data.
    /// </summary>
    /// <returns></returns>
    public string GetTempFileName()
    {
        string path = GetRootPaths(RootPaths.TempRoot);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return Path.Combine(path, Guid.NewGuid().ToString().Substring(0, 8));
    }

    /// <summary>
    /// Get the base Ahsoka directory path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string GetAhsokaPath(string path)
    {
        var rootPath = Environment.ExpandEnvironmentVariables(hardwareInformation.FactoryInfo.RootPath);
        var folder = $"{rootPath}/{path}";
        return folder;
    }
}

/// <summary>
/// Paths for Storing Non-Volatile Service Data.
/// </summary>
public enum ServiceDataPaths
{
    /// <summary>
    /// Path to Networking Data.
    /// </summary>
    NetworkProfileFolder,

}

/// <summary>
/// Paths used for the Installer and Installer Components
/// </summary>
public enum InstallerPaths
{
    /// <summary>
    /// Folder which is watched for installs to be installed
    /// </summary>
    WatchFolder,

    /// <summary>
    /// Folder where the Installer Application is Stored
    /// </summary>
    InstallerUIRoot,

    /// <summary>
    /// Path to the Installer UX Executable 
    /// </summary>
    InstallerUIExecutablePath,

    /// <summary>
    /// Directory where the System Command Line Tools are stored.
    /// </summary>
    CommandLineRoot,

    /// <summary>
    /// Path to the Command Line Executable.
    /// </summary>
    CommandLineExecutablePath,

    /// <summary>
    /// Path to the Boot Logo Image File.
    /// </summary>
    BootLogoFileName,

    /// <summary>
    /// Path to the Application Version File
    /// </summary>
    ApplicationVersion,

    /// <summary>
    /// Path to the Application Description File
    /// </summary>
    ApplicationDescription,

    /// <summary>
    ///  Path to the Installer Version File.
    /// </summary>
    InstallerVersion,

    /// <summary>
    /// Path to the Application Name File
    /// </summary>
    ApplicationName,

    /// <summary>
    /// Path to the Application Type File
    /// </summary>
    ApplicationType,

    /// <summary>
    /// Path to AutoStart Service Configuration if enabled.
    /// </summary>
    StartConfig,

    /// <summary>
    /// Path to the CoProcessor Customer Application
    /// </summary>
    CoProcessorApplicationPath,

    /// <summary>
    /// Signing Key used for Decrypting Applications
    /// </summary>
    ApplicationSigningKeyPath,
}

/// <summary>
/// Path to the Root Ahsoka Folders
/// </summary>
public enum RootPaths
{
    /// <summary>
    /// Path to the Customer Application
    /// </summary>
    ApplicationRoot,

    /// <summary>
    ///  Path to the last Installed Customer Application Backup
    /// </summary>
    BackupRoot,

    /// <summary>
    ///  Path to any Dependencies installed for a Development Platform.
    /// </summary>
    DependencyRoot,

    /// <summary>
    ///  Path to the Temp Files used by the System.
    /// </summary>
    TempRoot,

    /// <summary>
    /// Path to the Factory Information Folder
    /// </summary>
    FactoryRoot,

    /// <summary>
    /// Path to the Field Information Folder
    /// </summary>
    FieldRoot,

    /// <summary>
    /// Path where System Logs are stored.
    /// </summary>
    LogRoot,

    /// <summary>
    /// Path where Cloud Data is Stored.
    /// </summary>
    CloudRoot,

    /// <summary>
    /// Path to non-volatile Data is store for Services
    /// </summary>
    ServiceDataRoot,
}

/// <summary>
/// Paths for Cloud Data
/// </summary>
public enum CloudPaths
{
    /// <summary>
    /// Path to the Spark Subscription File.
    /// </summary>
    SparkSubscriptionFile,

    /// <summary>
    /// Path to the Spark Factory Subcription File.
    /// </summary>
    FactorySubscriptionFile,
}

/// <summary>
/// Path to the Factory Data Items
/// </summary>
public enum FactoryPaths
{
    /// <summary>
    /// Path to the Hardware Info Folder
    /// </summary>
    HardwareRoot,

    /// <summary>
    /// Path to the Hardware Information Folder.
    /// </summary>
    HardwareInformationFile,

    /// <summary>
    /// Path to the Hardware Device Signing Key
    /// </summary>
    HardwareDeviceSigningKeyFile,

    /// <summary>
    /// Path to the Hardware Device Signing Key
    /// </summary>
    HardwareDeviceSigningKeyPEMFile,

    /// <summary>
    /// Path to the Hardware Folder Signature Validation File.
    /// </summary>
    HardwarePackageValidationKeyPEMFile,

    /// <summary>
    /// Path to the Hardware Folder Manifest File
    /// </summary>
    HardwareManifestFile,

    /// <summary>
    /// Path to the Manifest Signature File which validates the Hardware Folder
    /// </summary>
    HardwareManifestSignatureFile,

    /// <summary>
    /// Path to the Application Startup File.
    /// </summary>
    ApplicationScriptFile,

    /// <summary>
    /// Path to the Hardware Public Key file used for encrypting packages.
    /// </summary>
    HardwareDeviceSigningPublicKeyFile,

    /// <summary>
    /// Path to the Hardware Public Key file used for encrypting packages.
    /// </summary>
    HardwareDeviceSigningPublicKeyPEMFile,

    /// <summary>
    /// Path to License Info for this Target
    /// </summary>
    ModuleLicenseFile
}

