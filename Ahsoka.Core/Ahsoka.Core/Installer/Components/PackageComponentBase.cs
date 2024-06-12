using Ahsoka.Installer.InstallEngine;
using System.IO;

namespace Ahsoka.Installer.Components;

// Base Component for Packaging Items in the Nuget Packages
internal abstract class PackageComponentBase : FileSystemComponentBase
{
    protected override string GetPackagingPath(string buildLocation, PackageInformation info)
    {
        string buildOutput = InstallerToolPaths.GetTargetRootPath(info.PlatformFamily);

        return Path.Combine(buildOutput, ComponentName);
    }
}
