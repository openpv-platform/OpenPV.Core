using Ahsoka.Services.Install;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;



namespace Ahsoka.Installer.InstallEngine;

/// <summary>
/// Component Base for Installer Slots
/// </summary>
public abstract class InstallEngineComponent
{
    #region Properties
    /// <summary>
    /// GUID Used to Track Components
    /// </summary>
    public abstract Guid ComponentType { get; }

    /// <summary>
    /// Name of the Component / Slot
    /// </summary>
    public abstract string ComponentName { get; }

    internal virtual bool IsEnabled { get; set; } = true;

    #endregion

    #region Public Methods
    internal PackageComponent Pack(string buildLocation,
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
        
        progress?.Report(new PackageProgressInfo() { Message = $"  Finished {ComponentName}" });

        return data;
    }

    internal void Cleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        OnCleanup(buildLocation, componentData, info, progress);
    }

    internal InstallResult InstallComponent(InstallContext context, PackageComponent item)
    {
        context.Progress?.Report(new InstallProgressInfo() { Message = $"Installing {ComponentName}", PercentComplete = 0 });
        var result = OnInstallComponent(context, item);
        context.Progress?.Report(new InstallProgressInfo() { Message = $"Installing {ComponentName}", PercentComplete = 100 });

        context.Log?.Report(new InstallLogInfo() { LogMessage = $"Install '{ComponentName}' Completed with Result: {result}", LogMessageType = LogMessageType.Information });

        return result;
    }
    #endregion

    #region Abstract
    /// <summary>
    /// Handles Creating the Package
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="info"></param>
    /// <param name="installedPlugins"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    protected abstract PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
        List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null);

    /// <summary>
    ///  Handles Cleanup of the Component after Packageing
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="componentData"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    protected abstract void OnCleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null);

    /// <summary>
    /// Handles any On Device Work involved in Installing the Component
    /// </summary>
    /// <param name="context"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    protected abstract InstallResult OnInstallComponent(InstallContext context, PackageComponent item);

    #endregion
}

