using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ahsoka.Installer.InstallEngine;

/// <summary>
/// Base Class for Plugins
/// </summary>
public class InstallerPlugin
{
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

    internal virtual void PrepareApplicationArea(ref string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress) { }

    /// <summary>
    /// Add any additional files required by the Application
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="attributes"></param>
    /// <param name="additionalFiles"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    internal virtual void AddAdditionalApplicationFiles(string buildLocation, 
        Dictionary<string, string> attributes, 
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info, 
        IProgress<PackageProgressInfo> progress) { }

    internal virtual void CleanupApplication(string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress) { }

    internal virtual void ModifyTargetInstall(string installDirectory, string applicationName) { }
}
