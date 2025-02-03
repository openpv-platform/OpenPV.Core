#pragma warning disable CS1591
using Ahsoka.Core.Utility;
using System;
using System.IO;

namespace Ahsoka.Core.Hardware;

public static class PlatformSupportPathInfo
{
    public const string ExtensionSupportFolderName = Extensions.ExtensionsFolder;
    public const string PlatformSupportRootFolderName = "PlatformSupport";
    public const string PlatformSupportRootFolderName_App = ".platform_support"; // Alternate name used in app folders
    static readonly string platformSupportFolder;
    static readonly string extensionSupportFolder;

    public static string PlatformSupportRootFolder => platformSupportFolder;

    static PlatformSupportPathInfo()
    {
        // Search for Root Folder Relative to Assembly first
        string path = AppContext.BaseDirectory;

        platformSupportFolder = FindPlatformSupportPath(path, PlatformSupportRootFolderName_App, PlatformSupportRootFolderName);
        extensionSupportFolder = FindPlatformSupportPath(path, ExtensionSupportFolderName);
    }

    public static string FindPlatformSupportPath(string rootPath, string folderName, string alternate = "UnknownTarget")
    {
        // Work our way up looking for platform support
        // if we find it (as a parent) OR we find a path that contains it (sibling of parent)
        // we stop.
        while (!String.IsNullOrEmpty(rootPath))
        {
            if (rootPath != null)
            {
                if (rootPath.EndsWith(folderName) || rootPath.EndsWith(alternate))
                    return rootPath; // Root is One Up from Platform Support Root.

                // Look to see if we are beside the folder
                string siblingPath = Path.Combine(rootPath, folderName);
                string siblingPath_alt = Path.Combine(rootPath, alternate);

                if (Directory.Exists(siblingPath))
                    return siblingPath;
                else if (Directory.Exists(siblingPath_alt))
                    return siblingPath_alt;
            }

            rootPath = new DirectoryInfo(rootPath).Parent?.FullName;
        }

        if (string.IsNullOrEmpty(rootPath) && folderName.ToLower().Contains("support"))
            return GetDeveloperPlatformSupportPath();

        return string.Empty;
    }

    public static string GetPlatformFolderName(PlatformFamily platform)
    {
        return $"{platform}_TargetSupport";
    }

    public static string GetPlatformSupportPath(PlatformFamily platform)
    {
        if (!Directory.Exists(platformSupportFolder))
            throw new ApplicationException($"The path to the PlatformSupport folder could not be found relative to {AppContext.BaseDirectory}.   Platform Support Not Supported");

        return Path.Combine(platformSupportFolder, GetPlatformFolderName(platform));
    }

    public static string GetPlatformResourcePath(PlatformFamily platform)
    {
        return Path.Combine(GetPlatformSupportPath(platform), "Resources");
    }

    public static string GetPlatformAppsPath(PlatformFamily platform)
    {
        return Path.Combine(GetPlatformSupportPath(platform), "Apps");
    }

    /// <summary>
    ///  Returns the path for the Customer Tools Support Folder (Desktops Only)
    /// </summary>
    /// <returns></returns>
    public static string GetDeveloperToolRootPath()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library", "Application Support", "AhsokaData");
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".AhsokaData");
        else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AhsokaData");
    }

    public static string GetDeveloperVersionRootPath()
    {
        var settingsFolder = GetDeveloperToolRootPath();
        var version = VersionUtility.GetAppVersion();
        return Path.Combine(settingsFolder, $"{version.Major}.0");
    }

    public static string GetDeveloperPlatformSupportPath()
    {
        return Path.Combine(GetDeveloperVersionRootPath(), "PlatformSupport");
    }
}
