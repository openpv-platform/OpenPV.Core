using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ahsoka.Installer.InstallEngine;

internal class InstallerPlugin
{
    internal static InstallerPlugin GetPlugin(UserInterfaceType userInterfaceType)
    {
        switch (userInterfaceType)
        {
            case UserInterfaceType.Storyboard: 
                return new StoryboardInstallerPlugin();
            default:
                return null;
        }
    }

    internal static InstallerPlugin GetPlugin(ApplicationType applicationType)
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

    internal virtual void CheckPrerequisites(PackageInformation packageInformation, IProgress<PackageProgressInfo> progress, CancellationToken token) { }

    internal virtual void PrepareApplicationArea(ref string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress) { }

    internal virtual void AddAdditionalApplicationFiles(string buildLocation, 
        Dictionary<string, string> attributes, 
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info, 
        IProgress<PackageProgressInfo> progress) { }

    internal virtual void CleanupApplication(string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress) { }

    internal virtual void ModifyTargetInstall(string installDirectory, string applicationName) { }
}
