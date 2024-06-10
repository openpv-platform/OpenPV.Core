using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;

namespace Ahsoka.Installer.Components;

/// <summary>
///  NOTE THAT THIS COMPONENT IS NO LONGER USED!!!
///  Dependencies are loaded during Prep / BitBake or with App Package.
/// </summary>
internal class DependencyComponent : PackageComponentBase
{
    public override Guid ComponentType => new("7BD2B8B0-49F7-4C86-8A38-CC40A2889379");

    public override bool IsRequiredComponent => true;

    public override string ComponentName => "Dependency";

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation,
       PackageInformation info,
       Dictionary<string, byte[]> additionalFiles,
       List<InstallerPlugin> installerPlugins,
       IProgress<PackageProgressInfo> progress)
    {
        return base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
    }

    protected override string GetTargetPath()
    {
        return SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.DependencyRoot);
    }
}
