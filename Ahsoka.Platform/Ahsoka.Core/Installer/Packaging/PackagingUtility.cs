#pragma warning disable CS1591
using Ahsoka.Services.Install;
using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Ahsoka.Installer;

public class PackagingUtility
{
    internal static string SelectApplicationTypeFolder(string pathToPackage, ApplicationType projectType)
    {
        // Select a Platorm Folder if Found
        string platform = Path.Combine(pathToPackage, projectType.ToString());
        if (Directory.Exists(platform))
            return platform;

        // Return Previous Path.. we will just put them all if we fail.
        return pathToPackage;
    }

    public static string SelectPlatformFolder(string pathToPackage, PlatformFamily family)
    {
        // Select a Platorm Folder if Found
        string platform = Path.Combine(pathToPackage, family.ToString());
        if (Directory.Exists(platform))
            return platform;

        // Fall back to an Empty Folder if Not Found.
        string any = Path.Combine(pathToPackage, System.PlatformFamily.Any.ToString());
        if (Directory.Exists(any))
            return any;

        // Return Null since no Valid Platform Folder was found.
        // This is to ensure we don't pick up mulitple folders
        return null;
    }

    public static void CreateZipPackage(string pathToPackage, string outputFile, PlatformFamily family, IProgress<InstallProgressInfo> progress = null, Dictionary<string, byte[]> additions = null)
    {
        CreateZipPackage(new string[] { pathToPackage }, outputFile, family, progress, additions);
    }

    public static void CreateZipPackage(string[] packageLocations, string outputFile, PlatformFamily family, IProgress<InstallProgressInfo> progress = null, Dictionary<string, byte[]> additions = null)
    {
        if (File.Exists(outputFile))
            File.Delete(outputFile);

        using var archive = ZipFile.Open(outputFile, ZipArchiveMode.Create);
        foreach (var pathToPackage in packageLocations)
        {
            var files = Directory.GetFiles(pathToPackage, "*", SearchOption.AllDirectories);
            foreach (var fileItem in files)
            {
                // Filter Runtime Folders
                if (!fileItem.Contains("runtimes") || fileItem.Contains($"runtimes{Path.DirectorySeparatorChar}{family.GetRuntimeId()}"))
                {
                    string path = Path.GetRelativePath(pathToPackage, fileItem).Replace("\\", "/");
                    archive.CreateEntryFromFile(fileItem, path);

                    progress?.Report(new InstallProgressInfo() { Message = " Adding " + path });
                }
            }
        }
        if (additions != null)
            foreach (var addition in additions)
            {
                var entry = archive.CreateEntry(addition.Key);
                using var stream = entry.Open();
                stream.Write(addition.Value);
            }
    }

    public static void ExtractPackage(string path,
        string outputPath,
        IProgress<InstallProgressInfo> progress,
        IProgress<InstallLogInfo> log)
    {
        var fileInfo = new FileInfo(path);
        var fileSize = fileInfo.Length;
        long filePosition = 0;
        long percentComplete;

        // ZipFile.ExtractToDirectory(path, outputPath, true);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (!String.IsNullOrEmpty(entry.Name))
            {
                filePosition += entry.CompressedLength;

                // Filter Runtime Folders
                string pathToFile = Path.Combine(outputPath, entry.FullName);
                string directory = Path.GetDirectoryName(pathToFile);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                entry.ExtractToFile(pathToFile, true);
                File.SetLastWriteTime(pathToFile, DateTime.Now);

                percentComplete = (int)((filePosition / (float)fileSize) * 100);
                log?.Report(new InstallLogInfo() { LogMessage = $"   Extracted {entry.FullName}", LogMessageType = LogMessageType.Information });
                progress?.Report(new InstallProgressInfo() { UpdatePercentOnly = true, PercentComplete = (int)percentComplete });
            }
        }
    }
}