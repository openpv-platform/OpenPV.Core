
using Ahsoka.BSPUpgrade;
using Ahsoka.Utility;
using DiscUtils;
using System.Text;
using static NetMQ.NetMQSelector;

namespace Ahsoka.CompareDistros;

internal class DistroCompareUtility
{
    static string fromVer = string.Empty;
    static string toVer = string.Empty;
    const string versionKey = @"usr\share\bsp-info\bsp-version";
       
    public void CompareDistroFolders(string origin, string destination, string outputFolder)
    {
        if (Directory.Exists(outputFolder))
            Directory.Delete(outputFolder,true);
        
        Directory.CreateDirectory(outputFolder);

        foreach (var destinationFileToCompare in Directory.GetFiles(destination, "*.ext4").OrderBy(x=>x)) // Relies on this to ensure our versions are found in first file.
        {
            string ext4File = Path.GetFileName(destinationFileToCompare);
            string originFileToCompare = Path.Combine(origin, ext4File);
            
            if (!File.Exists(originFileToCompare))
            {
                Console.Write($"Could not compare folders since the file {originFileToCompare} does not exist");
                return;
            }
            
            CompareFiles(originFileToCompare, destinationFileToCompare, Path.Combine(outputFolder, ext4File));
            Console.WriteLine();
        }
    }

    private void CompareFiles(string originFileToCompare, string destinationFileToCompare, string outputFolder)
    {
        // Analyse Input File
        FileStream fileStreamOrigin = new(originFileToCompare, FileMode.Open);
        DiscUtils.Ext.ExtFileSystem originFs = new(fileStreamOrigin);
        Dictionary<string, UpgradeFileInfo> originFileList = new();
        SearchDirectory(originFs, originFs.Root, originFileList);

        // Analyse Output File
        FileStream fileStreamDestination = new(destinationFileToCompare, FileMode.Open);
        DiscUtils.Ext.ExtFileSystem destinationFs = new(fileStreamDestination);
        Dictionary<string, UpgradeFileInfo> destinationFileList = new();
        SearchDirectory(destinationFs, destinationFs.Root, destinationFileList);

        if (originFileList.ContainsKey(versionKey))
        {
            var fromVersion = originFileList[versionKey];
            using Stream originVersion = originFs.GetFileInfo(fromVersion.FilePath).OpenRead();
            byte[] fromVerBytes = new byte[fromVersion.FileSize];
            originVersion.ReadExactly(fromVerBytes, 0, fromVerBytes.Length);
            fromVer = Encoding.UTF8.GetString(fromVerBytes).Trim();

            var toVersion = destinationFileList[versionKey];
            using Stream destVersion = destinationFs.GetFileInfo(fromVersion.FilePath).OpenRead();
            byte[] toVerBytes = new byte[toVersion.FileSize];
            destVersion.ReadExactly(toVerBytes, 0, toVerBytes.Length);
            toVer = Encoding.UTF8.GetString(toVerBytes).Trim();
        }

        Console.WriteLine($"Processing {Path.GetFileName(destinationFileToCompare)} for volume {destinationFs.VolumeLabel} ");
        UpgradeVolumeInfo volumeInfo = new()
        {
            FromVersion = fromVer,
            ToVersion = toVer,
            VolumeLabel = destinationFs.VolumeLabel,
            RootPath = destinationFs.Root.FullName
        };

        List<UpgradeFileInfo> filesToLeave = new(); // We will discard these

        // Search for Files Not in Destination (delete)
        foreach (var originFile in originFileList)
        {
            if (!destinationFileList.ContainsKey(originFile.Key)) // For Sure Deletes
                volumeInfo.DeletedFiles.Add(originFile.Value);
        }

        // Search for Files Not in Origin (new) or Different (Updates)
        foreach (var destinationFile in destinationFileList)
        {
            if (originFileList.TryGetValue(destinationFile.Key, out UpgradeFileInfo fileInfo))
            {
                if (fileInfo.IsSymlink &&
                    fileInfo.SymlinkPath == destinationFile.Value.SymlinkPath)
                {
                    // Same Link
                }
                else if (fileInfo.SymlinkPath != string.Empty &&
                    destinationFile.Value.SymlinkPath == string.Empty)
                {
                    // Was a link..now isn't so add
                    volumeInfo.AddedFiles.Add(destinationFile.Value);
                }
                else if (fileInfo.SymlinkPath == string.Empty &&
                    destinationFile.Value.SymlinkPath != string.Empty)
                {
                    // Was a file, now is a link so add as Link
                    volumeInfo.AddedFiles.Add(destinationFile.Value);
                }
                else if (fileInfo.FileSize != destinationFile.Value.FileSize || 
                    fileInfo.Permissions  != destinationFile.Value.Permissions ) // Real Files
                {
                    // File Size Different so Add
                    volumeInfo.AddedFiles.Add(destinationFile.Value);// Different Size so Add
                }
                else // Not a link and needs more work
                {
                    filesToLeave.Add(destinationFile.Value); // Needs full Comparision
                }
            }
            else
                volumeInfo.AddedFiles.Add(destinationFile.Value);// New File so Adds
        }

        long bytesComplete = 0;
        long bytesTotal = filesToLeave.Sum(x => x.FileSize);
        int percentComplete = (int)(((float)bytesComplete / (float)bytesTotal) * 100);
        int lastPercent = 0;
        foreach (var file in filesToLeave.ToArray())
        {
            using Stream originFile = originFs.GetFileInfo(file.FilePath).OpenRead();
            using Stream destFile = destinationFs.GetFileInfo(file.FilePath).OpenRead();

            // If !Same, Add to Upgrade List
            if (!AreStreamsEqual(originFile, destFile))
            {
                volumeInfo.AddedFiles.Add(file);
                filesToLeave.Remove(file);
            }

            bytesComplete += file.FileSize;
            percentComplete = (int)(((float)bytesComplete / (float)bytesTotal) * 100);
            if (percentComplete != lastPercent && percentComplete % 5 == 0)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Processing {percentComplete}% Complete" );
                lastPercent = percentComplete;
            }
        }

        bytesComplete = 0;
        bytesTotal = volumeInfo.AddedFiles.Sum(x => x.FileSize);
        percentComplete = (int)(((float)bytesComplete / (float)bytesTotal) * 100);
        lastPercent = 0;

        foreach (var item in volumeInfo.AddedFiles)
        {
            // Write the Physical Files
            // Symlinks only end up in the metadata files.
            if (!item.IsSymlink)
            {
                string outputFile = Path.Combine(outputFolder, item.FilePath.ToString());

                // Write File
                using Stream destFile = destinationFs.GetFileInfo(item.FilePath).OpenRead();
                byte[] bytes = new byte[item.FileSize];
                destFile.ReadExactly(bytes, 0, bytes.Length);

                string directory = Path.GetDirectoryName(outputFile);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                bytesComplete += item.FileSize;
                percentComplete = (int)(((float)bytesComplete / (float)bytesTotal) * 100);
                if (percentComplete != lastPercent && percentComplete % 5 == 0)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"Writing {percentComplete}% Complete");
                    lastPercent = percentComplete;
                }

                File.WriteAllBytes(outputFile, bytes);
            }
        }

        string fileName = Path.Combine(Path.GetDirectoryName(outputFolder), Path.GetFileNameWithoutExtension(destinationFileToCompare) + ".pbuff");
        using FileStream fs = new(fileName, FileMode.Create);
        ProtoBuf.Serializer.Serialize(fs, volumeInfo);

        fileName = Path.Combine(Path.GetDirectoryName(outputFolder), Path.GetFileNameWithoutExtension(destinationFileToCompare) + ".json");
        File.WriteAllText(fileName, JsonUtility.Serialize(volumeInfo));

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine($"Results {volumeInfo.AddedFiles.Count} New Files, {volumeInfo.DeletedFiles.Count} Deleted Files and {filesToLeave.Count} To Leave (Same)");
        Console.WriteLine();

        string scriptName = Path.Combine(Path.GetDirectoryName(outputFolder), Path.GetFileNameWithoutExtension(destinationFileToCompare) + ".sh");

        GenerateVolumeUpgrade(volumeInfo, scriptName, fromVer, "/rootfs/upgrade_temp/", $"/{volumeInfo.VolumeLabel}/");
    }

    private void GenerateVolumeUpgrade(UpgradeVolumeInfo volumeInfo, string scriptName, string fromVersion, string originMount, string destinationMount)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"#!/bin/sh");
        builder.AppendLine();
   
        // Check Version and Return if Not Matching
      
        builder.AppendLine($"fromVerCheck=$(cat /usr/share/bsp-info/bsp-version | grep -c {fromVer})");
        builder.AppendLine("if [ fromVerCheck -eq 0 ]; then");

        // Execute Deletes
        builder.AppendLine();
        builder.AppendLine("    #Executing Deletes");
        foreach (var delete in volumeInfo.DeletedFiles)
        {
            // Execute Delete 
            builder.AppendLine($"    rm -f '{destinationMount}{delete.FilePath.Replace("\\","/")}'");
        }

        // Execute Adds
        builder.AppendLine();
        builder.AppendLine("    #Execute Copies / Adds and Set Permissions");
        foreach (var addedItem in volumeInfo.AddedFiles.Where(x=>x.IsSymlink == false))
        {
            // Execute MV from Temp to New
            // Execute Delete 
            builder.AppendLine($"    cp -f '{originMount}{addedItem.FilePath.Replace("\\", "/")}' '{destinationMount}{addedItem.FilePath.Replace("\\", "/")}'");
            builder.AppendLine($"    chmod {(int)addedItem.Permissions} '{destinationMount}{addedItem.FilePath.Replace("\\", "/")}'");
            builder.AppendLine();
        }


        // Execute Symlinks
        builder.AppendLine();
        builder.AppendLine("    #Create Symlinks");
        foreach (var addedItem in volumeInfo.AddedFiles.Where(x => x.IsSymlink))
        {
            // Look for Links in Same Directory
            if (!addedItem.SymlinkPath.Contains("/") && !addedItem.SymlinkPath.Contains("\\"))
                addedItem.SymlinkPath =  Path.GetDirectoryName(addedItem.FilePath).Replace("\\", "/") + "/" + addedItem.SymlinkPath;

            // Create Link / Overwrite if exists.
            builder.AppendLine($"    ln -sf '{destinationMount}{addedItem.SymlinkPath.TrimStart('/')}' '{destinationMount}{addedItem.FilePath.Replace("\\", "/")}'");
        }


        builder.AppendLine();
        builder.AppendLine("else ");
        builder.AppendLine($"   echo 'Incorrect Upgrade Version.  This package can only be applied to BSP Version {fromVer}'");
        builder.AppendLine("fi ");

        File.WriteAllText(scriptName, builder.ToString().ReplaceLineEndings("\n"));

    }

    private static void SearchDirectory(DiscUtils.Ext.ExtFileSystem fs, DiscFileSystemInfo dir, Dictionary<string, UpgradeFileInfo> fileList)
    {
        try
        {
            foreach (var item in fs.GetDirectoryInfo(dir.FullName)
                                   .GetFileSystemInfos())
            {
                if (item.Attributes.HasFlag(FileAttributes.Directory))
                {
                    SearchDirectory(fs, item, fileList);
                }
                else if (item.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    var info2 = fs.GetSymlinkTarget(item.FullName);
                    fileList.Add(item.FullName, new UpgradeFileInfo() 
                    { 
                        FilePath = item.FullName,  
                        IsSymlink = true, 
                        SymlinkPath = info2
                    });
                }
                else if (item.Attributes.HasFlag(FileAttributes.Normal))
                {
                    var linuxFileInfo = fs.GetUnixFileInfo(item.FullName);
                    var length = fs.GetFileLength(item.FullName);
                    fileList.Add(item.FullName, new UpgradeFileInfo()
                    {
                        FilePath = item.FullName,
                        FileSize = length,
                        GroupId = linuxFileInfo.GroupId,
                        UserId = linuxFileInfo.UserId,
                        Permissions = (UpgradeFilePermissions)linuxFileInfo.Permissions,
                    });
                }
                else
                {
                    throw new Exception("Not Handled File Type " + item.Attributes);
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR" + ex.Message);
        }
    }

    private bool AreStreamsEqual(Stream stream, Stream other)
    {
        byte[] originBuffer = new byte[stream.Length];
        stream.ReadExactly(originBuffer, 0, (int)stream.Length);

        byte[] destBuffer = new byte[other.Length];
        other.ReadExactly(destBuffer, 0, (int)other.Length);

        return destBuffer.SequenceEqual(originBuffer);
    }
}