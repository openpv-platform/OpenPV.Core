using Ahsoka.Spark;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ahsoka.System;

internal class WindowsTargetTools : RemoteTargetTool
{
    protected override bool OnPrepTarget(HardwareInfo hardwareInfo,
        ITargetConnectionInfo connectionInfo,
        SparkConnectionInfo sparkInfo,
        IProgress<string> progress,
        ManufacturingInfo manufacturingInfo = null)
    {
        // Clear FileSystme
        if (!tokenSource.IsCancellationRequested)
        {
            ClearLocalMachine(hardwareInfo);
        }

        // PushHardwareInfo(info, sshClient, scpClient);
        if (!tokenSource.IsCancellationRequested)
        {
            PushLocalMachineHardwareInfo(hardwareInfo);
        }

        return true;
    }

    private void ClearLocalMachine(HardwareInfo info)
    {
        progress.Report("Removing Existing Provisioning Data...");

        string pathToRemove = $"{Environment.ExpandEnvironmentVariables(info.FactoryInfo.RootPath)}";
        if (Directory.Exists(pathToRemove))
            Directory.Delete(pathToRemove, true);
    }

    private void PushLocalMachineHardwareInfo(HardwareInfo info)
    {
        Dictionary<string, MemoryStream> filesToLoad = new();

        // Assign Hardware Serial Number and Generate DeviceID
        info.FactoryInfo.DeviceId = Guid.NewGuid();
        string hardwareID = info.FactoryInfo.DeviceId.ToString(); // Returns the SN.

        // Add Factory Hardware Folder
        string factoryPath = info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareRoot);
        progress.Report("");
        progress.Report($"Creating Factory Info Folder at {factoryPath}");
        Directory.CreateDirectory(factoryPath);

        progress.Report($"Pushing new Factory Info File for Platform {info.FactoryInfo.PlatformFamily}...");

        // Add Factory Info to Upload List
        string factoryFileData = JsonUtility.Serialize(info);
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareInformationFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(factoryFileData)));

        // Create KEY Pair
        var result = RSAEncryption.GenerateNewKeyPair(false);
        var privateKey = result.Key;
        var publicKey = result.Value;

        // Add Spark Keys to List
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(privateKey)));
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningPublicKeyFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(publicKey)));

        // Create 
        var resultPEM = RSAEncryption.GenerateNewKeyPair(true);
        privateKey = resultPEM.Key;
        publicKey = resultPEM.Value;

        // Add Spark Keys to List
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyPEMFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(privateKey)));
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningPublicKeyPEMFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(publicKey)));

        // Create Manifest FileInfo
        FactoryManifest manifest = new()
        {
            DeviceId = hardwareID,
            DevicePublicKey = publicKey
        };

        // Create Files in Manifest.
        foreach (var fileItem in filesToLoad)
        {
            fileItem.Value.Seek(0, SeekOrigin.Begin);

            manifest.FileInfo.Add(new()
            {
                CheckSum = RSAEncryption.GetChecksum(fileItem.Value),
                Path = fileItem.Key
            });
        }

        // Update Factory Folder with Manifest
        var manifestData = UTF8Encoding.UTF8.GetBytes(JsonUtility.Serialize(manifest));
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareManifestFile), new MemoryStream(manifestData));

        // Signature is not included in 
        var signature = RSAEncryption.GetSignatureWithKey(manifestData, Properties.Resources.UntrustedPrivateKeyPEM);
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareManifestSignatureFile), new MemoryStream(signature));

        // Upload Factory Info.
        foreach (var item in filesToLoad)
        {
            item.Value.Seek(0, SeekOrigin.Begin);
            File.WriteAllBytes(item.Key, item.Value.ToArray());
        }
    }
}
