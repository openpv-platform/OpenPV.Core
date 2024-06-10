#pragma warning disable CS1591
using System;
using System.IO;

namespace Ahsoka.System.Hardware;

public static class PlatformSupportPathInfo
{
    public const string PlatformSupportRootFolderName = "PlatformSupport";
    static readonly string platformSupportFolder;

    static PlatformSupportPathInfo()
    {
        // Search for Root Folder Relative to Assembly first
        string path = AppContext.BaseDirectory;

        // Work our way up looking for platform support
        // if we find it (as a parent) OR we find a path that contains it (sibling of parent)
        // we stop.
        while (!String.IsNullOrEmpty(path))
        {
            path = new DirectoryInfo(path).Parent?.FullName;

            if (path != null)
            {
                if (path.EndsWith(PlatformSupportRootFolderName))
                {
                    platformSupportFolder = path; // Root is One Up from Platform Support Root.
                    break;
                }

                // Look to see if we are beside the folder
                string siblingPath = Path.Combine(path, PlatformSupportRootFolderName);
                if (Directory.Exists(siblingPath))
                {
                    platformSupportFolder = siblingPath; // Root is One Up from Platform Support Root.
                    break;
                }
            }
        }

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
}
