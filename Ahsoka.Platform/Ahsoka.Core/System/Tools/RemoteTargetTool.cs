#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.Installer.Components;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Spark;
using Ahsoka.Services.System;
using Ahsoka.Spark;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;

namespace Ahsoka.System;

internal class RemoteTargetTool : ICanCancel, IRemoteTargetTool
{
    const string ReleaseName = "System Updates";
    protected IProgress<string> progress;
    protected readonly CancellationTokenSource tokenSource = new();

    public bool Prep(ITargetConnectionInfo connectionInfo,
        SparkConnectionInfo sparkInfo,
        IProgress<string> progress,
        ManufacturingInfo manufacturingInfo = null) // Leave Null for Customer Prep...requires Spark Admin or Provisioning Rights.
    {
        this.progress = progress;

        string installerPath = InstallerToolPaths.GetInstallerPackagePath(connectionInfo.PlatformFamily);
        if (!Directory.Exists(installerPath))
        {
            progress.Report($"Path to Installer (Command Line) was not found {installerPath} in {Environment.CurrentDirectory}");
            return false;
        }

        // Fetch the Hardware Info
        HardwareInfo hardwareInfo = HardwareInfo.GetHardwareInfo(connectionInfo.PlatformFamily, connectionInfo.PlatformQualifier);

        try
        {
           return OnPrepTarget(hardwareInfo, connectionInfo,sparkInfo,progress,manufacturingInfo);
        }
        catch { }
      
        return false;
    }

    protected virtual bool OnPrepTarget(HardwareInfo hardwareInfo, 
        ITargetConnectionInfo connectionInfo, 
        SparkConnectionInfo sparkInfo,
        IProgress<string> progress,
        ManufacturingInfo manufacturingInfo = null)
    {
        // Connect to Device.  Allow One Timeout in case we just rebooted and net wasn't ready.
        SshClient sshClient;
        try { sshClient = SshUtility.CreateSshClient(connectionInfo); }
        catch (Exception) { sshClient = SshUtility.CreateSshClient(connectionInfo); }

        ScpClient scpClient = SshUtility.CreateScpClient(connectionInfo);

        bool isExactMatch = false;

        try
        {
            if (IsCompatibleFirmware(hardwareInfo, sshClient, out isExactMatch, progress))
                return PrepDisplayInternal(hardwareInfo, sshClient, scpClient, sparkInfo, manufacturingInfo);
        }
        finally
        {
            sshClient.Disconnect();
            scpClient.Disconnect();

            if (!isExactMatch)
                progress.Report("\r\n***** Information - While compatible, the currently installed BSP is older than the latest available BSP Distribution. *****");
        }

        return true;
    }

    protected bool PrepDisplayInternal(HardwareInfo info,
       SshClient sshClient,
       ScpClient scpClient,
       SparkConnectionInfo sparkInfo,
       ManufacturingInfo manufacturingInfo = null)
    {
        string uiInstallerPath = InstallerToolPaths.GetInstallerUIPackagePath(info.PlatformFamily);
        if (!Directory.Exists(uiInstallerPath))
        {
            progress.Report($"Path to InstallerUI was not found {uiInstallerPath} in {Environment.CurrentDirectory}");
            return false;
        }

        string commandLinePath = InstallerToolPaths.GetInstallerPackagePath(info.PlatformFamily);
        if (!Directory.Exists(uiInstallerPath))
        {
            progress.Report($"Path to Installer (Command Line) was not found {commandLinePath} in {Environment.CurrentDirectory}");
            return false;
        }

        // Validate Spark Connection
        if (manufacturingInfo != null)
            sparkInfo.LoginToProvisioningClient();
        else
            sparkInfo.LoginToTeamClient();

        // Clear Folder if Already Prepped
        ClearAndStopInstaller(info, sshClient);

        if (!tokenSource.IsCancellationRequested)
            RestartResizehelper(sshClient);

        // Push Command Line 
        if (!tokenSource.IsCancellationRequested)
            PushFactoryExecutable(info, sshClient, scpClient,
                commandLinePath,
                info.TargetPathInfo.GetInstallerPath(InstallerPaths.CommandLineRoot),
                info.TargetPathInfo.GetInstallerPath(InstallerPaths.CommandLineExecutablePath));

        // Prep the Factory / Hardware Folder
        if (!tokenSource.IsCancellationRequested)
            if (!PushHardwareInfo(info, sshClient, scpClient,
                 sparkInfo,
                 manufacturingInfo))
                return false; // Hardware Mismatch or Other Issue

        if (!tokenSource.IsCancellationRequested && info.PlatformFamily == PlatformFamily.OpenViewLinux)
            TempExecuteBSPMods(info, sshClient, scpClient);

        // Prep the Factory / InstallerUI Folder
        if (!tokenSource.IsCancellationRequested)
            PushFactoryExecutable(info, sshClient, scpClient,
                uiInstallerPath,
                info.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIRoot),
                info.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIExecutablePath));

        // Push the Start Script
        if (!tokenSource.IsCancellationRequested)
            PushStartScript(info, sshClient, scpClient);

        // Temp Code to Push Override to Wayland Start Script
        if (!tokenSource.IsCancellationRequested)
            TempInstallScript(info, sshClient, scpClient);

        if (!tokenSource.IsCancellationRequested)
            NetworkFirmwareComponent.GetPlatformFirmware(info)?.InstallRemote(info, sshClient, scpClient, progress);

        // Reboot
        if (!tokenSource.IsCancellationRequested)
            Reboot(info, sshClient, scpClient);

        return !tokenSource.IsCancellationRequested;
    }

    private void RestartResizehelper(SshClient sshClient)
    {
        progress.Report("Restarting Resize Helper");

        string command = $"systemctl restart resize-helper";
        var result = sshClient.RunCommand($"{command}");

        if (result.Error != string.Empty)
            throw new Exception(result.Result);
    }

    private void ClearAndStopInstaller(HardwareInfo info, SshClient sshClient)
    {
        progress.Report("Removing Existing Provisioning Data...");

        string pathToRemove = $"{info.FactoryInfo.RootPath}";
        var result = sshClient.RunCommand($"rm -r {pathToRemove}");

        if (result.Error != string.Empty && !result.Error.Contains("No such"))
            throw new Exception(result.Result);

        // We ignore Errors in case its not running.
        progress.Report("Stop Application if Running...");
        _ = sshClient.RunCommand($"systemctl kill Ahsoka.Application");

        Thread.Sleep(1000);

        progress.Report("Stop Installer if Running...");
        _ = sshClient.RunCommand($"systemctl kill Ahsoka.Installer");
    }

    private bool PushHardwareInfo(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        SparkConnectionInfo sparkInfo,
        ManufacturingInfo manufacturingInfo = null )
    {
        bool factoryDataIsValid = false;

        Dictionary<string, MemoryStream> filesToLoad = new();

        string hardwareID = GetHardwareId(info, sshClient);

        // Create Device Signing Files in XML Format
        var result = RSAEncryption.GenerateNewKeyPair(false);
        var privateKey = result.Key;
        var publicKey = result.Value;

        // Capture for use in Spark Provisioning Later
        var sparkPublicKey = publicKey;


        string factoryFileData;

        // Manage Manufacturing Info
        if (manufacturingInfo != null)
        {
            Guid deviceId = FactoryInfo.CreateDeviceId(info.PlatformFamily, hardwareID);

            info.FactoryInfo.PartNumber = manufacturingInfo.PartNumber;
            info.FactoryInfo.SerialNumber = manufacturingInfo.SerialNumber;
            info.FactoryInfo.MacAddress = manufacturingInfo.MacAddress;
            info.FactoryInfo.DeviceId = deviceId;

            // Serialize New Hardware Info to store in the Cloud....as a backup.
            factoryFileData = JsonUtility.Serialize(info);

            var newFactoryInfo = new DeviceFactoryInfoDTO()
            {
                DeviceID = info.FactoryInfo.DeviceId,
                DeviceHardwareID = hardwareID,
                MacAddress = manufacturingInfo.MacAddress,
                PartNumber = manufacturingInfo.PartNumber,
                SerialNumber = manufacturingInfo.SerialNumber,
                PlatformFamily = info.PlatformFamily.ToString(),
                PlatformQualifier = info.PlatformQualifier,
                FactoryInfo = factoryFileData,
                DevicePublicKey = sparkPublicKey,
            };

            var factoryResultVerification = SparkProvisioningClient.SharedSparkClient.CreateFactoryInfo(newFactoryInfo);

            bool isSaved = newFactoryInfo.SerialNumber == factoryResultVerification.SerialNumber &&
                          newFactoryInfo.PartNumber == factoryResultVerification.PartNumber &&
                          newFactoryInfo.MacAddress == factoryResultVerification.MacAddress &&
                          newFactoryInfo.DeviceHardwareID == factoryResultVerification.DeviceHardwareID &&
                          newFactoryInfo.DeviceID == factoryResultVerification.DeviceID &&
                          newFactoryInfo.DevicePublicKey == factoryResultVerification.DevicePublicKey;

            if (!isSaved)
                throw new ApplicationException("\r\nThe Factory Info Was not Completely Saved...Erroring Out.");

            factoryDataIsValid = true;
        }
        else
        {
            DeviceFactoryInfoDTO factoryInfo = null;
            try
            {
                factoryInfo = SparkAccountClient.SharedSparkClient.GetFactoryInfo(info.PlatformFamily.ToString(), hardwareID);
                factoryDataIsValid = factoryInfo.DeviceID != Guid.Empty;
            }
            catch { } // Ignore if No Factory Info or Error.  Data will show "Invalid" on Installer UI.


            if (factoryInfo != null)
            {
                bool isMatch = factoryInfo.PlatformFamily == info.PlatformFamily.ToString() &&
                               factoryInfo.PlatformQualifier == info.PlatformQualifier;

                // Continue for Ubuntu since its our fake Test Platform
                if (!isMatch && !(info.PlatformFamily == PlatformFamily.Ubuntu64 && info.PlatformQualifier == "Desktop"))
                {
                    progress.Report($"\r\n***** Error - The factory Family / Qualifier ({factoryInfo.PlatformFamily}) / {factoryInfo.PlatformQualifier} does not match the specified family and qualifier ({info.PlatformFamily} / {info.PlatformQualifier}) *****\r\n");
                    return false;
                }
            }

            // We didn't get valid data from Spark but use a real device id to be compatible with Spark etc.
            if (!factoryDataIsValid)
            {
                factoryInfo = new()
                {
                    DeviceID = FactoryInfo.CreateDeviceId(info.PlatformFamily, hardwareID),
                    PartNumber = "1111111",
                    SerialNumber = "22222222"
                };
            }

            // Update to Factory Values.
            info.FactoryInfo.PartNumber = factoryInfo.PartNumber;
            info.FactoryInfo.SerialNumber = factoryInfo.SerialNumber;
            info.FactoryInfo.MacAddress = factoryInfo.MacAddress;
            info.FactoryInfo.DeviceId = factoryInfo.DeviceID;

            factoryFileData = JsonUtility.Serialize(info);
        }

        // Add Factory Hardware Folder
        string factoryPath = info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareRoot);
        progress.Report("");
        progress.Report($"Creating Factory Info Folder at {factoryPath}");
        var commandResult = sshClient.RunCommand($"mkdir -p {factoryPath}");

        if (commandResult.Error != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Pushing new Factory Info File for Platform {info.FactoryInfo.PlatformFamily}...");

        // Add Factory Info to Upload List
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareInformationFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(factoryFileData)));

       
        // Add Keys to List - NOTE THIS IS TO SUPPORT PRE 2.0 Hardware Prep
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(privateKey)));
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningPublicKeyFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(publicKey)));

        // Create 
        privateKey = RSAEncryption.ConvertXMLToPrivatePEM(privateKey);
        publicKey = RSAEncryption.ConvertXMLToPublicPEM(publicKey);

        // Add Keys to List - SUPPORTS 2.0+
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyPEMFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(privateKey)));
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningPublicKeyPEMFile), new MemoryStream(UTF8Encoding.UTF8.GetBytes(publicKey)));

        // Ad PowerVision Public Key to Display...this is the untrusted key used for integrity checking.
        var publicPackageKey = UTF8Encoding.UTF8.GetBytes(Properties.Resources.UntrustedPublicKeyPEM);
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwarePackageValidationKeyPEMFile), new MemoryStream(publicPackageKey));

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

            manifest.FileInfo.Add(new FactoryManifestFileInfo()
            {
                CheckSum = RSAEncryption.GetChecksum(fileItem.Value),
                Path = fileItem.Key
            });
        }

        // Update Factory Folder with Manifest
        var manifestData = UTF8Encoding.UTF8.GetBytes(JsonUtility.Serialize(manifest));
        filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareManifestFile), new MemoryStream(manifestData));

        // Add Signature if Factory Data is Valid
        if (factoryDataIsValid)
        {
            var signature = RSAEncryption.GetSignatureWithKey(manifestData, Properties.Resources.UntrustedPrivateKeyPEM);
            filesToLoad.Add(info.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareManifestSignatureFile), new MemoryStream(signature));
        }
        else
        {
            progress.Report($"*************************************************************************************************");
            progress.Report($"****** WARNING - Device has not been Factory Provisioned. Some Features May NOT Work ************");
            progress.Report($"*************************************************************************************************");
        }

        // Upload Factory Info.
        foreach (var item in filesToLoad)
        {
            item.Value.Seek(0, SeekOrigin.Begin);
            scpClient.Upload(item.Value, item.Key);
        }

        // Note that Ahsoka.Manufacturing.DLL parses this output.
        progress.Report($"Hardware ID: {hardwareID}");
        progress.Report($"Device ID: {info.FactoryInfo.DeviceId}");

        // Provision Display in Spark with Local Key
        SparkSubscriptionInfo subInfo = new()
        {
            DeviceId = info.FactoryInfo.DeviceId.ToString(),
            DeviceKey = sparkPublicKey,
            PartNumber = info.FactoryInfo.PartNumber,
            SerialNumber = info.FactoryInfo.SerialNumber,
        };

        return InstallExistingLicenseData(info, subInfo, sshClient, scpClient, sparkInfo, progress);
    }

    private bool InstallExistingLicenseData(HardwareInfo info,
          SparkSubscriptionInfo subInfo,
          SshClient sshClient,
          ScpClient scpClient,
          SparkConnectionInfo sparkInfo,
          IProgress<string> progress)
    {
        // Clear Auth and use Teams Login
        var oldAuth = sparkInfo.CurrentAuthorization;
        sparkInfo.CurrentAuthorization = null;
        sparkInfo.LoginToTeamClient();

        // Provision Display in Spark with Local Key
        ProvisionDisplayRequest provisionDisplayRequest = new()
        {
            ApplicationId = Guid.Empty,
            PackageName = ReleaseName
        };

        progress.Report($"Requesting License File from Cloud");
        var moduleInfo = SparkCustomerTools.PushSparkLicenseInfo(subInfo, Guid.Empty, Guid.Empty, provisionDisplayRequest, progress, true);

        // Push License File to Display
        progress.Report($"Restoring Module License File from Cloud");
        string moduleLicenseFile = info.TargetPathInfo.GetFactoryPath(FactoryPaths.ModuleLicenseFile);
        using (var fileToUpload = new MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(fileToUpload, moduleInfo);
            fileToUpload.Seek(0, SeekOrigin.Begin);
            scpClient.Upload(fileToUpload, moduleLicenseFile);
        };

        sparkInfo.CurrentAuthorization = oldAuth;

        return true;
    }

    public string GetHardwareId(HardwareInfo info, SshClient sshClient)
    {
        // Run Command on Unit to Get SN.  This is the only method that will work without a signed factory folder.
        string pathToExecutable = info.TargetPathInfo.GetInstallerPath(InstallerPaths.CommandLineExecutablePath);

        SshCommand commandResult = sshClient.RunCommand($"{pathToExecutable} --GetHardwareSerialNumber");
        if (commandResult.Error != string.Empty)
            throw new Exception(commandResult.Error);

        //// Extract Id from Result
        string hardwareIdResult = commandResult.Result;

        int startPos = hardwareIdResult.IndexOf(RemoteTargetToolFactory.HardwareIdPrefix) + RemoteTargetToolFactory.HardwareIdPrefix.Length;
        int length = hardwareIdResult.Length - startPos; // Next Charage Return

        return hardwareIdResult.Substring(startPos, length).TrimEnd().Replace("\0", "");
    }

    protected void PushFactoryExecutable(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        string pathToZip = "",
        string pathOnDisplay = "",
        string pathToExecutable = "")
    {
        progress.Report("");
        progress.Report($"Creating Installer Folder at {pathOnDisplay}");
        var commandResult = sshClient.RunCommand($"mkdir -p {pathOnDisplay}");

        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        string fileNameToCleanUp = Path.GetTempFileName();
        try
        {
            progress.Report($"Creating Executable Package for Display");
            var installProgress = new LocalProgress<Services.Install.InstallProgressInfo>(info =>
            {
                progress.Report($"  {info.Message}");
            });

            // Pack File to Zip
            PackagingUtility.CreateZipPackage(pathToZip, fileNameToCleanUp, info.FactoryInfo.PlatformFamily, installProgress);

            using (var fileToUpload = new FileStream(fileNameToCleanUp, FileMode.Open))
            {
                progress.Report("");
                progress.Report($"Uploading Package");
                string installerZipFileTargetPath = $"{pathOnDisplay}/Package.zip";
                int percentComplete = 0;
                EventHandler<ScpUploadEventArgs> handler = (s, e) =>
                {
                    int newPercent = (int)(e.Uploaded / (float)e.Size * 100);
                    if (newPercent != percentComplete && newPercent % 5 == 0)
                    {
                        progress.Report($"   {percentComplete}% Uploaded");
                        percentComplete = newPercent;
                    }
                };

                scpClient.Uploading += handler;
                scpClient.Upload(fileToUpload, installerZipFileTargetPath);
                scpClient.Uploading -= handler;

                progress.Report($"   100% Uploaded");
                progress.Report("");
                progress.Report($"Unzipping Package on Target");
                var unzipInstall = sshClient.RunCommand($"unzip {installerZipFileTargetPath} -d {pathOnDisplay}");
                progress.Report(unzipInstall.Result);
                if (unzipInstall.ExitStatus != 0)
                    throw new Exception($"Unable to unzip {installerZipFileTargetPath}: {unzipInstall.Error}");

                progress.Report($"Removing Temp File");
                var deleteInstallFile = sshClient.RunCommand($"rm {installerZipFileTargetPath}");

                if (deleteInstallFile.Error != string.Empty)
                    throw new Exception(deleteInstallFile.Result);
            }

            progress.Report("");
            progress.Report($"Granting Execute to Installer UI");
            var enableStart = sshClient.RunCommand($"chmod +x {pathToExecutable}");
            if (enableStart.Error != string.Empty)
                throw new Exception(enableStart.Result);
        }
        finally
        {
            // Clean up Temp File
            if (File.Exists(fileNameToCleanUp))
                File.Delete(fileNameToCleanUp);
        }
    }

    private void PushStartScript(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient)
    {
        progress.Report("");
        progress.Report($"Adding Start Script");
        string startScriptPath = $"{info.TargetPathInfo.GetRootPaths(RootPaths.FactoryRoot)}/start.sh";
        string startScriptFileContents = Properties.Resources.Ahsoka_Installer_Start;

        // Adding start script for chain of command to start field InstallerUI from future application package
        string execCommand = Path.GetFileName(info.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIExecutablePath));
        string factoryRoot = info.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIRoot);
        string fieldRoot = info.TargetPathInfo.GetInstallerPath(InstallerPaths.InstallerUIRoot, false);

        startScriptFileContents = startScriptFileContents.Replace("${ExecCommand}", execCommand);
        startScriptFileContents = startScriptFileContents.Replace("${FieldInstallerFolder}", fieldRoot);
        startScriptFileContents = startScriptFileContents.Replace("${FactoryInstallerFolder}", factoryRoot);

        using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(startScriptFileContents)))
            scpClient.Upload(memoryStream, startScriptPath);

        progress.Report($"Granting Execute to Install Script");
        var commandResult = sshClient.RunCommand($"chmod +x {startScriptPath}");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);
    }

    public void Cancel()
    {
        tokenSource.Cancel();
    }

    private void Reboot(HardwareInfo info,
                         SshClient sshClient,
                         ScpClient scpClient)
    {
        // Reboot Display
        progress.Report($"Reboot Display in 5 Seconds");
        _ = sshClient.RunCommand($"(sleep 5;/sbin/reboot)&");
    }

    private void TempInstallScript(HardwareInfo info,
                          SshClient sshClient,
                          ScpClient scpClient)
    {
        progress.Report("");
        progress.Report($"Disable Ahsoka Services");
        var commandResult = sshClient.RunCommand($"systemctl disable Ahsoka.Application.service");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        commandResult = sshClient.RunCommand($"systemctl disable Ahsoka.Installer.service");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Remove Ahsoka Services");
        commandResult = sshClient.RunCommand($"rm -f /etc/systemd/system/Ahsoka.*");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Adding Ahsoka Installer Service");
        string unitFilePath = $"/etc/systemd/system/Ahsoka.Installer.service";
        using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.Ahsoka_Installer)))
            scpClient.Upload(memoryStream, unitFilePath);

        progress.Report($"Create System Service");
        commandResult = sshClient.RunCommand($"chmod 644 {unitFilePath}");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Refresh Services");
        commandResult = sshClient.RunCommand($"systemctl daemon-reload");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Enable System Service");
        commandResult = sshClient.RunCommand($"systemctl enable Ahsoka.Installer.service");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Restore Logo");
        sshClient.RunCommand($"export PATH=$PATH:/sbin;lcd-config");

    }

    // Platform Specific logic.
    [ExcludeFromCodeCoverage]
    private SshCommand TempExecuteBSPMods(HardwareInfo info,
                          SshClient sshClient, ScpClient scpClient)
    {
        string ahsokaDepFolder = info.TargetPathInfo.GetRootPaths(RootPaths.DependencyRoot);
        string ahsokaDepExe = ahsokaDepFolder + "/dotnet/dotnet";

        progress.Report($"Remove Ahsoka Dependencies");
        var commandResult = sshClient.RunCommand($"rm -r {ahsokaDepFolder}");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        string depPath = Path.Combine(InstallerToolPaths.GetTargetRootPath(info.PlatformFamily), "Dependency");

        if (!Directory.Exists(depPath))
            throw new Exception($"Path to Ahsoka dependencies was not found {depPath}");

        // Upload Dependencies (dotnet)
        PushFactoryExecutable(info, sshClient, scpClient,
                depPath,
                ahsokaDepFolder,
                ahsokaDepExe);

        scpClient.Upload(new MemoryStream(Properties.Resources.LinuxAppProfile), "/etc/profile.d/app_profile.sh");

        progress.Report($"Remove systemd generators");
        commandResult = sshClient.RunCommand($"rm -f /lib/systemd/system-generators/system-generator-alsa-*");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        commandResult = sshClient.RunCommand($"rm -f /lib/systemd/system-generators/systemd-fstab-generator");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report($"Disable ESP Utils");
        commandResult = sshClient.RunCommand($"systemctl disable esp-firmware");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        commandResult = sshClient.RunCommand($"systemctl disable esp-devnode");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);


        progress.Report($"Disable Netdata");
        commandResult = sshClient.RunCommand($"systemctl stop netdata");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        commandResult = sshClient.RunCommand($"systemctl disable netdata");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        // TODO: Consider moving this to the Hardware Def.
        string pathToJournalOverride = "/lib/systemd/journald.conf.d/02-openview.conf";
        scpClient.Upload(new MemoryStream(Properties.Resources.JournalOverride), pathToJournalOverride);

        return commandResult;
    }

    protected bool IsProvisioned(SshClient client)
    {
        var commandResult = client.RunCommand("ls /usr/local/Ahsoka/");
        return commandResult.Error == string.Empty;
    }

    protected bool IsCompatibleFirmware(HardwareInfo info, SshClient client, out bool isExactMatch, IProgress<string> progress)
    {
        var prereqs = FirmwarePreRequisites.GetBSPInfo(info.PlatformFamily);

        var commandResult = client.RunCommand("bsp-info -v");
        string bspVersion = commandResult.Result.Remove(0, "BSP Version:".Length).Trim();

        commandResult = client.RunCommand("bsp-info -a");
        string bspInfo = commandResult.Result;

        if (bspVersion.CompareTo(prereqs.MinVersion) >= 0 && bspInfo.Contains(prereqs.RequiredCodeName))
        {
            isExactMatch = bspVersion.StartsWith(prereqs.FirmwareFilter);
            progress.Report($"Prep is compatible with {bspVersion} bsp version.  Continuing...");
            return true;
        }
        else
        {
            isExactMatch = false;
            progress.Report($"Prep is incompatible with {bspVersion} bsp version.");
            progress.Report($"Please update the target to {prereqs.MinVersion} of {prereqs.RequiredCodeName} or newer Firmware in Add-Ins.");
            return false;
        }
    }

    public string GetInstallerJournal(ITargetConnectionInfo info)
    {
        var sshClient = SshUtility.CreateSshClient(info);

        var enableStart = sshClient.RunCommand("journalctl -u Ahsoka.Installer.service");
        if (enableStart.Result != string.Empty)
            return enableStart.Result;
        else
            return "Command Was Not Successful";
    }

    public string GetApplicationJournal(IConnectionInfo info)
    {
        var sshClient = SshUtility.CreateSshClient(info);

        var enableStart = sshClient.RunCommand("journalctl -u Ahsoka.Application.service");
        if (enableStart.Result != string.Empty)
            return enableStart.Result;
        else
            return "Command Was Not Successful";
    }

    public string ClearJournal(IConnectionInfo info)
    {
        var sshClient = SshUtility.CreateSshClient(info);

        var flush = sshClient.RunCommand("journalctl --flush --rotate");
        var clear = sshClient.RunCommand("journalctl --vacuum-time=1s");
        if (flush.ExitStatus == 0 && clear.ExitStatus == 0)
            return "Journal Flush Completed";
        else
            return "Command Was Not Successful";
    }

    public string Reboot(IConnectionInfo info)
    {
        try
        {
            var sshClient = SshUtility.CreateSshClient(info);

            var enableStart = sshClient.RunCommand("(sleep 5;/sbin/reboot)&");
            if (enableStart.Result != string.Empty)
                return enableStart.Result;
            else
                return "Command Was Not Successful";
        }
        catch { return "Command Was Not Successful"; }
    }

    public string StopApplication(IConnectionInfo info, bool isInstaller)
    {
        try
        {
            var sshClient = SshUtility.CreateSshClient(info);

            string command = $"systemctl stop Ahsoka.Application";
            if (isInstaller)
                command = $"systemctl stop Ahsoka.Installer ; systemctl start Ahsoka.Application";

            var enableStart = sshClient.RunCommand(command);
            if (enableStart.Result != string.Empty)
                return enableStart.Result;
            else
                return "Command Was Not Successful";
        }
        catch { return "Command Was Not Successful"; }
    }

    public GetHardwareInformationReply ReturnToInstaller(TargetConnectionInfo connectionInfo, SystemServiceClient client, CancellationTokenSource source, IProgress<string> progressString)
    {
        progressString.Report("The target is currently running an application...Rebooting to Installer.\r\n");

        // Restart the Installer
        StopApplication(connectionInfo, false);

        progressString.Report("Waiting for Installer (60s Remaining)");

        // We wait for 60s 
        int count = 60;
        var hw = client.GetHardwareInformation(2000);

        // Wait up to 60s for the Installer To To Return.
        while ((hw == null || !hw.IsInstaller)
                 && count > 0
                 && !source.IsCancellationRequested)
        {
            count -= 2;

            try
            {
                progressString.Report($"Waiting for Installer ({count}s Remaining)");
                hw = client.GetHardwareInformation(2000);
            }
            catch (Exception) { }

            if (source.IsCancellationRequested)
                return hw;
        }

        return hw;
    }

   
}

public class KeyFileInfo
{
    public string ConfigPath { get; init; }
    public string Name { get; init; }
    public string PrivateKeyPath { get; init; }
    public string PublicKeyPath { get => $"{PrivateKeyPath}.pub"; }

    public bool IsLocal
    {
        get { return File.Exists(PrivateKeyPath) && File.Exists(PublicKeyPath); }
    }
}