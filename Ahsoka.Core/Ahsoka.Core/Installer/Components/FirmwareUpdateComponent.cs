using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Ahsoka.Installer.Components;

internal class FirmwareUpdateComponent : FileSystemComponentBase
{
    public const string PackageName = "FirmwareUpdate";
    public override Guid ComponentType => new("171A5303-F6BC-403E-B123-19792F1FE3E2");
    public override string ComponentName => PackageName;

    public string UpdatePath { get; internal set; }

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation,
        PackageInformation info,
        Dictionary<string, byte[]> additionalFiles,
        List<InstallerPlugin> installerPlugins,
        IProgress<PackageProgressInfo> progress)
    {
        if (this.IsEnabled)
        {

            if (UpdatePath == null || !File.Exists(UpdatePath))
            {
                string error = $"Could not find an appropriate firmware upgrade file for {info.PlatformFamily}.";
                throw new ApplicationException(error);
            }

            return new PackageComponent()
            {
                ComponentType = ComponentType,
                Data = new FileStream(UpdatePath, FileMode.Open),
                Version = VersionUtility.GetAppVersionString()
            };
        }
        else
        {
            return null;
        }
    }

    protected override void OnCleanup(string buildLocation, PackageComponent componentData, PackageInformation info, IProgress<PackageProgressInfo> progress = null)
    {
        base.OnCleanup(buildLocation, componentData, info, progress);
    }

    protected override string GetTargetPath()
    {
        return SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.FirmwareUpgradePath, false);
    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {
        var result = base.OnInstallComponent(context, item);

        string pathToRun = Path.Combine(GetTargetPath(), "upgrade.sh");
        if (File.Exists(pathToRun))
        {
            ProcessUtility.RunProcess("/bin/sh", $"-c \"chmod +x {pathToRun}", GetTargetPath(), out string log, out string error);
            context.Progress.Report(new InstallProgressInfo() { Message = log });

            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                context.Progress.Report(new() { Message = info.Data });
            });

            ProcessUtility.RunProcessAsync("/bin/sh", $"{pathToRun}", GetTargetPath(), progressText, CancellationToken.None);
        }

        return result;
    }
}
