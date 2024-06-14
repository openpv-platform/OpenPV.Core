#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using System;
using System.IO;
using System.Reflection;

namespace Ahsoka.System.Hardware;

public static class PlatformSupportPathInfo
{
    public const string ExtensionSupportFolderName = Extensions.ExtensionsFolder;
    public const string PlatformSupportRootFolderName = "PlatformSupport";
    static readonly string platformSupportFolder;
    static readonly string extensionSupportFolder;

    public static string PlatformSupportRootFolder => platformSupportFolder;

    static PlatformSupportPathInfo()
    {
        // Search for Root Folder Relative to Assembly first
        string path = AppContext.BaseDirectory;

        platformSupportFolder = FindPlatformSupportPath(path, PlatformSupportRootFolderName);
        extensionSupportFolder = FindPlatformSupportPath(path, ExtensionSupportFolderName);
    }

    public static string FindPlatformSupportPath(string rootPath, string folderName)
    {
        // Work our way up looking for platform support
        // if we find it (as a parent) OR we find a path that contains it (sibling of parent)
        // we stop.
        while (!String.IsNullOrEmpty(rootPath))
        {
            rootPath = new DirectoryInfo(rootPath).Parent?.FullName;

            if (rootPath != null)
            {
                if (rootPath.EndsWith(folderName))
                {
                    return rootPath; // Root is One Up from Platform Support Root.
                }

                // Look to see if we are beside the folder
                string siblingPath = Path.Combine(rootPath, folderName);
                if (Directory.Exists(siblingPath))
                {
                    return siblingPath; // Root is One Up from Platform Support Root.
                }
            }
        }

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

    public static string GetExtensionSupportFolder()
    {
        string returnValue = extensionSupportFolder;

        // Fallback to local "Extensions" folder
        if (!Directory.Exists(returnValue))
        {
            Console.WriteLine(AppContext.BaseDirectory); 
            returnValue = Path.Combine(AppContext.BaseDirectory, Extensions.ExtensionsFolder);
        }

        if (!Directory.Exists(returnValue))
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"The path to the Extensions folder could not be found relative to {AppContext.BaseDirectory}.   Extension Support Not Available");

        return returnValue;
    }

    /// <summary>
    ///  Returns the path for the Customer Tools Support Folder (Desktops Only)
    /// </summary>
    /// <returns></returns>
    public static string GetDeveloperToolPath()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library", "Application Support", "AhsokaData");
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".AhsokaData");
        else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AhsokaData");
    }

}
