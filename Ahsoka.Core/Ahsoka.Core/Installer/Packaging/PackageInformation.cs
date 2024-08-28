#pragma warning disable CS1591
using Ahsoka.System;
using Ahsoka.Utility;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Ahsoka.Installer;

public class PackageInformation : PackageHeader
{
    string originalFilePath;

    // Package Type
    public string Version { get; set; }
    public ApplicationType ApplicationType { get; set; } = ApplicationType.Dotnet;
    public Orientation SystemOrientation { get; set; } = Orientation.DefaultOrientation;
    public string ExecCommand { get; set; }
    public string PackageName { get; set; }
    public string PackageDescription { get; set; }
    public string ApplicationVersion { get; set; }
    public string PackageApplicationFolder { get; set; }
    public string PackageOutputDirectory { get; set; }
    public string StartupLogoPath { get; set; }
    public string PackageSigningCertFile { get; set; }
    public List<ModuleLicense> Licenses { get; set; } = new();

    public bool IncludeApplication { get; set; } = false;

    public bool IncludeBootLogo { get; set; } = false;

    public bool IncludeInstallerUI { get; set; } = false;

    public bool IncludeFirmwareUpdate { get; set; } = false;

    public bool OptimizedServiceManager { get; set; } = true;

    public DotNetInformation DotNetInfo { get; set; }

    public CppInformation CppInfo { get; set; }

    public ServiceInfo ServiceInfo { get; set; } = new();

    public static PackageInformation LoadPackageInformation(string path)
    {
        PackageInformation package;
        if (path.ToLower().EndsWith("opvpackage"))
        {
            // Extract Package Info to Temp File (delete when done
            using FileStream zipFile = new FileStream(path, FileMode.Open);
            using ZipArchive archive = new ZipArchive(zipFile);
            var entry = archive.Entries.FirstOrDefault(x => x.Name.ToLower().EndsWith("packageinfo.json"));
            string filePath = Path.GetTempFileName();
            entry.ExtractToFile(filePath,true);
            package = ConfigurationFileLoader.LoadFile<PackageInformation>(filePath);
            File.Delete(filePath);
        }
        else
             package = ConfigurationFileLoader.LoadFile<PackageInformation>(path);

        package.Version = VersionUtility.GetAppVersionString();
        package.originalFilePath = path;
        return package;
    }

    public string GetApplicationFullPath()
    {
        // Build Path Relative to File Location
        string path = Path.Combine(Path.GetDirectoryName(originalFilePath), PackageApplicationFolder);
        path = Path.GetFullPath(path);
        return path;
    }

    public string GetOutputFullPath(bool isCustomPackage = false)
    {
        if (PackageOutputDirectory == null)
            return String.Empty;

        string path = Path.Combine(Path.GetDirectoryName(originalFilePath), PackageOutputDirectory);
        if (isCustomPackage)
            return Path.Combine(path, PackageName + ".custom" +PackageType.GetFileExtension());
        else
            return Path.Combine(path, PackageName + PackageType.GetFileExtension());
    }

    public string GetOutputRelativePath(bool isCustomPackage = false)
    {
        return Path.GetRelativePath(Path.GetDirectoryName(originalFilePath), GetOutputFullPath(isCustomPackage));
    }

    internal PackageHeader CopyAsPackageHeader(string signingKey)
    {
        // Serialize and Deserialise as Header to elimate inherited values.
        var retVal = JsonUtility.Deserialize<PackageHeader>(JsonUtility.Serialize(this));

        if (retVal.SecurityModelNumber?.Length > 0)
            retVal.SecurityModelNumberData = RSAEncryption.EncryptWithPEMKey(UTF8Encoding.UTF8.GetBytes(retVal.SecurityModelNumber), signingKey);

        if (retVal.SecurityPassKey?.Length > 0)
            retVal.SecurityPassKeyData = RSAEncryption.EncryptWithPEMKey(UTF8Encoding.UTF8.GetBytes(retVal.SecurityPassKey), signingKey);

        if (retVal.SecuritySerialNumber?.Length > 0)
            retVal.SecuritySerialNumberData = RSAEncryption.EncryptWithPEMKey(UTF8Encoding.UTF8.GetBytes(retVal.SecuritySerialNumber), signingKey);

        return retVal;
    }

    public string GetPackageInfoPath()
    {
        return originalFilePath;
    }
    
    public string GetPackageCertPath()
    {
        return GetPackageCertPath(this.PackageSigningCertFile, KeyTypes.PackageSigningKey);
    }

    public static string GetPackageCertPath(string keyName, KeyTypes type)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "AhsokaData",
            "AhsokaCerts", 
            (type == KeyTypes.PackageSigningKey ? keyName + ".appcert" : keyName + ".servicecert"));
    }

    internal string GetPackageExtentionFolder()
    {
        if (ApplicationType == Installer.ApplicationType.Dotnet)
            return Path.Combine(Path.GetDirectoryName(GetPackageInfoPath()),"obj", Extensions.ExtensionsFolder);
        else
            return Path.Combine(Path.GetDirectoryName(GetPackageInfoPath()), Extensions.ExtensionsFolder);

    }

    internal bool CanUseOptimizerdServiceManager()
    {
        ProcessUtility.RunProcess("dotnet", "--list-sdks", null, out string result, out string error);
        return result.Contains("8.0.") && OptimizedServiceManager && (PlatformFamily is PlatformFamily.OpenViewLinux or PlatformFamily.OpenViewLinuxPro);
    }
}

public class DotNetInformation
{
    public string ProjectPath { get; set; }
    public string PublishProfile { get; set; }
}

public class CppInformation
{
    public string ProjectPath { get; set; }
}

public class ServiceInfo
{
    public RuntimeConfiguration RuntimeConfiguration { get; set; } = new();

    public List<StartupEnvironmentValue> EnvironmentValues { get; set; } = new();
}

public class StartupEnvironmentValue
{
    public string VariableName { get; set; }
    public string VariableValue { get; set; }
}

public class DeviceKeyInfo
{
    public KeyTypes KeyTypes { get; set; }
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
}

public enum KeyTypes
{
    DeviceKey,
    PackageSigningKey,
    ServiceKey,
}
