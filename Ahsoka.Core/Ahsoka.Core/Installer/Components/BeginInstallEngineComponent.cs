using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ahsoka.Installer.Components;

internal class BeginInstallEngineComponent : InstallEngineComponent
{
    #region Properties
    public override Guid ComponentType => new("D0BE7C9D-084B-40E9-9716-452053B751C1");

    public override string ComponentName => "Begin Install";
    #endregion

    #region Methods
    protected override PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
        List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null)
    {
        return new PackageComponent()
        {
            ComponentType = ComponentType,
            Data = new MemoryStream(UTF8Encoding.UTF8.GetBytes(ComponentType.ToString())),
            Version = VersionUtility.GetAppVersionString()
        };
    }

    protected override void OnCleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        // No cleanup memory due to memory stream
    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {

        // Remove Current App Data
        string appDataPath = ApplicationDataPaths.GetLocalDataPath();
        if (Directory.Exists(appDataPath))
        {
            Directory.Delete(appDataPath, true);
            context.Log.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Removing Local App Data @ {appDataPath}" });
        }

        string dependencyRoot = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.DependencyRoot);
        if (!Directory.Exists(dependencyRoot))
        {
            Directory.CreateDirectory(dependencyRoot);
            context.Log.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"System Root Created at {dependencyRoot}" });
        }

        string factoryRoot = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.FactoryRoot);
        if (!Directory.Exists(factoryRoot))
        {
            Directory.CreateDirectory(factoryRoot);
            context.Log.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Factory Root Created at {factoryRoot}" });
        }

        return InstallResult.Success;
    }

    #endregion
}
