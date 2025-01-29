using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Installer.Components;

internal abstract class FileSystemComponentBase : InstallEngineComponent
{
    #region Fields
    string fileNameToCleanUp = null;
    #endregion

    #region Properties
    public virtual bool FilterPlatformFolder { get; } = true;

    public virtual bool FilterProjectTypeFolder { get; } = true;
    #endregion

    #region Packaging Functions
    protected virtual string GetPackagingPath(string buildLocation, PackageInformation info)
    {
        return buildLocation;
    }

    protected override sealed PackageComponent OnCreatePackageStream(string buildLocation, PackageInformation info, List<InstallerPlugin> installerPlugins, IProgress<PackageProgressInfo> progress = null)
    {
        Dictionary<string, byte[]> additionalFiles = new();
        return OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
    }

    protected virtual PackageComponent OnCreateFilePackageStream(string buildLocation,
        PackageInformation info,
        Dictionary<string, byte[]> additionalFiles,
        List<InstallerPlugin> installerPlugins,
        IProgress<PackageProgressInfo> progress)
    {
        string pathToPackage = GetPackagingPath(buildLocation, info);

        if (Directory.Exists(pathToPackage))
        {
            fileNameToCleanUp = Path.GetTempFileName();

            PackagingUtility.CreateZipPackage(pathToPackage, fileNameToCleanUp, info.PlatformFamily, null, additionalFiles);

            return new PackageComponent()
            {
                ComponentType = ComponentType,
                Data = new FileStream(fileNameToCleanUp, FileMode.Open),
                Version = VersionUtility.GetAppVersionString()
            };
        }

        return null;
    }

    protected override void OnCleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        // Close the File Stream
        using (componentData.Data) { }

        // Delete the Temp
        if (fileNameToCleanUp != null)
            File.Delete(fileNameToCleanUp);
    }
    #endregion

    #region Installation Functions
    protected abstract string GetTargetPath();

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {
        context.Log?.Report(new InstallLogInfo() { LogMessage = $"Component '{ComponentName}' Began Extraction", LogMessageType = LogMessageType.Information });

        // Get Output Path for Component.
        string outputPath = GetTargetPath();
        if (String.IsNullOrEmpty(outputPath))
        {
            context.Log?.Report(new InstallLogInfo() { LogMessage = $"Component '{ComponentName}' Contained No Data to Install", LogMessageType = LogMessageType.Information });
            return InstallResult.Success;
        }

        // Remove Old Path Before Extraction
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);
            context.Log?.Report(new InstallLogInfo() { LogMessage = $"Output Directory Cleared {outputPath}", LogMessageType = LogMessageType.Information });
        }

        // Create Temp File Name
        string path = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName() + ".zip";
        using (var fs = new FileStream(path, FileMode.OpenOrCreate))
            context.InstallEngine.ExtractPackage(this.ComponentType, fs, context.Progress);

        // Extract Zip to Output Area
        PackagingUtility.ExtractPackage(path, outputPath, context.Progress, context.Log);

        // Remove Temp File
        File.Delete(path);

        context.Log?.Report(new InstallLogInfo() { LogMessage = $"Component '{ComponentName}' Completed Extraction", LogMessageType = LogMessageType.Information });

        return InstallResult.Success;
    }
    #endregion
}
