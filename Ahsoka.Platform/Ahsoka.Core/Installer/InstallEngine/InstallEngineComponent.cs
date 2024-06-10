using Ahsoka.Services.Install;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;



namespace Ahsoka.Installer.InstallEngine;


internal abstract class InstallEngineComponent
{
    #region Properties
    public abstract Guid ComponentType { get; }

    public abstract string ComponentName { get; }

    public abstract bool IsRequiredComponent { get; }
    #endregion

    #region Public Methods
    public PackageComponent Pack(string buildLocation,
        List<InstallerPlugin> installedPlugins,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        progress?.Report(new PackageProgressInfo() { Message = $"  Packing {ComponentName}" });

        PackageComponent data = OnCreatePackageStream(buildLocation,
            info,
            installedPlugins,
            progress);

        if (data != null)
        {
            if (data.ComponentType != ComponentType)
                throw new ApplicationException("Component Type was not set on the Package Component");
        }
        else if (this.IsRequiredComponent) // Debug Mode Skips Empty Packages
        {
            throw new ApplicationException($"The component {this.ComponentName} is required and was not found.");
        }

        progress?.Report(new PackageProgressInfo() { Message = $"  Finished {ComponentName}" });

        return data;
    }

    public void Cleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        OnCleanup(buildLocation, componentData, info, progress);
    }

    public InstallResult InstallComponent(InstallContext context, PackageComponent item)
    {
        context.Progress?.Report(new InstallProgressInfo() { Message = $"Installing {ComponentName}", PercentComplete = 0 });
        var result = OnInstallComponent(context, item);
        context.Progress?.Report(new InstallProgressInfo() { Message = $"Installing {ComponentName}", PercentComplete = 100 });

        context.Log?.Report(new InstallLogInfo() { LogMessage = $"Install '{ComponentName}' Completed with Result: {result}", LogMessageType = LogMessageType.Information });

        return result;
    }
    #endregion

    #region Abstract
    protected abstract PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
        List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null);

    protected abstract void OnCleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null);

    protected abstract InstallResult OnInstallComponent(InstallContext context, PackageComponent item);

    #endregion
}

