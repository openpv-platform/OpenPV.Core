#pragma warning disable CS1591
using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Installer.Components;
using System.IO;

namespace Ahsoka.Installer.InstallEngine;

public class InstallerToolPaths
{
    const string toolRootFolderName = "PlatformSupport";

    public static string GetInstallerUIPackagePath(PlatformFamily family)
    {
        return Path.Combine(GetTargetRootPath(family), InstallerUIComponent.PackageName);
    }

    public static string GetInstallerPackagePath(PlatformFamily family)
    {
        return Path.Combine(GetTargetRootPath(family), CommandLineComponent.PackageName);
    }

    public static string GetTargetRootPath(PlatformFamily family)
    {
        // Return the Target Support Folder
        return PlatformSupportPathInfo.GetPlatformSupportPath(family);
    }

}
