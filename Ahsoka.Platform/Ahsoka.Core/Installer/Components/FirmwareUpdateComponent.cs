using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Installer.Components;

internal class FirmwareUpdateComponent : FileSystemComponentBase
{
    private string tempFolderToClean = string.Empty;
   
    public const string PackageName = "FirmwareUpdate";
    public override Guid ComponentType => new("171A5303-F6BC-403E-B123-19792F1FE3E2");
    public override bool IsRequiredComponent => false;
    public override string ComponentName => PackageName;

    protected override string GetPackagingPath(string buildLocation, PackageInformation info)
    {
        return tempFolderToClean;
    }

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation,
        PackageInformation info,
        Dictionary<string, byte[]> additionalFiles,
        List<InstallerPlugin> installerPlugins,
        IProgress<PackageProgressInfo> progress)
    {
        if (info.IncludeFirmwareUpdate)
        {
            tempFolderToClean = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolderToClean);

            // Find BSP Upgrade and OTA Boot Files File
            string path = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(info.PlatformFamily), "Firmware");
            string updatePath = Directory.GetFiles(path, "upgrade.*.zip", SearchOption.AllDirectories).FirstOrDefault();
            string otaPath = Directory.GetFiles(path, "ota.*.zip", SearchOption.AllDirectories).FirstOrDefault();

            if (otaPath == null || updatePath == null)
            {
                string error = $"Could not find an appropriate firmware upgrade file for {info.PlatformFamily}.";
                throw new ApplicationException(error);
            }

            // Combine Files into new Temp Path
            PackagingUtility.ExtractPackage(updatePath, tempFolderToClean, null, null);
            PackagingUtility.ExtractPackage(otaPath, tempFolderToClean, null, null);

            return base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
        }
        else
        {
            return null;
        }
    }

    protected override void OnCleanup(string buildLocation, PackageComponent componentData, PackageInformation info, IProgress<PackageProgressInfo> progress = null)
    {
        // Delete Temp Area.
        Directory.Delete(tempFolderToClean, true);

        base.OnCleanup(buildLocation, componentData, info, progress);
    }

    protected override string GetTargetPath()
    {
        return string.Empty; // SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIRoot, false);
    }
}
