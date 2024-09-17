using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;

namespace Ahsoka.Installer.Components;

internal class InstallerUIComponent : PackageComponentBase
{
    public const string PackageName = "InstallerUI";

    public override Guid ComponentType => new("4194A78B-953F-414E-BBBB-C2D33B5F6C75");

    public override string ComponentName => PackageName;

    public string InstallerPath { get; internal set; }

    protected override string GetPackagingPath(string buildLocation, PackageInformation info)
    {
        return InstallerPath;
    }

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation,
        PackageInformation info,
        Dictionary<string, byte[]> additionalFiles,
        List<InstallerPlugin> installerPlugins,
        IProgress<PackageProgressInfo> progress)
    {
        if (IsEnabled)
        {
            return base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
        }
        else
        {
            return null;
        }
    }

    protected override string GetTargetPath()
    {
        return SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIRoot, false);
    }
}
