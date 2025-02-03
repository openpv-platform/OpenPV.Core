using Ahsoka.Core;
using Ahsoka.Services.Install;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ahsoka.Installer.InstallEngine;

/// <summary>
/// Base Class for Plugins
/// </summary>
public class InstallerPlugin
{
    /// <summary>
    /// GUID Used to Track Components
    /// </summary>
    public virtual List<PlatformFamily> SupportedPlatforms { get; } = [PlatformFamily.Any];

    internal static InstallerPlugin GetApplicationPlugin(ApplicationType applicationType)
    {
        switch (applicationType)
        {
            case ApplicationType.Dotnet:
                return new DotNetInstallerPlugin();
            case ApplicationType.CODESYS:
                return new CodesysInstallerPlugin();
            default:
                return null;
        }
    }

    /// <summary>
    /// Check the App Package for any PreRequisites or Licenses
    /// </summary>
    /// <param name="packageInformation"></param>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    public virtual void CheckPrerequisites(PackageInformation packageInformation, IProgress<PackageProgressInfo> progress, CancellationToken token) { }

    /// <summary>
    /// Prepare the Application Folder Prior to adding to Package
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    public virtual void PrepareApplicationArea(ref string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress) { }

    /// <summary>
    /// Add any additional files required by the Application
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="attributes"></param>
    /// <param name="additionalFiles"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    public virtual void AddAdditionalApplicationFiles(string buildLocation,
        Dictionary<string, string> attributes,
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress)
    { }

    /// <summary>
    /// Clean the App Area after Building
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    public virtual void CleanupApplication(string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress) { }

    /// <summary>
    /// Make any Target Modfications
    /// </summary>
    /// <param name="installDirectory"></param>
    /// <param name="applicationName"></param>
    public virtual void ModifyTargetInstall(string installDirectory, string applicationName) { }

    internal static List<InstallerPlugin> GetExtensionPlugins(PlatformFamily platformFamily)
    {
        return Extensions.GetExtensions<InstallerPlugin>().Where(x => x.SupportedPlatforms.Contains(platformFamily) || x.SupportedPlatforms.Contains(PlatformFamily.Any)).ToList();
    }
}
