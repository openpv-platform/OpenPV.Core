#pragma warning disable CS1591
using Ahsoka.Core.Hardware;
using Ahsoka.Services.Device.Handlers;
using Renci.SshNet;
using System;
using System.IO;

namespace Ahsoka.Core;


internal class OpenViewRemoteTargetTool : LinuxRemoteTargetTool
{
    protected override void BeforePrep(SshClient sshClient)
    {
        if (!tokenSource.IsCancellationRequested)
            RestartResizehelper(sshClient);
    }

    private void RestartResizehelper(SshClient sshClient)
    {
        progress.Report("Restarting Resize Helper");

        string command = $"systemctl restart resize-helper";
        var result = sshClient.RunCommand($"{command}");

        if (result.Error != string.Empty)
            throw new Exception(result.Result);
    }

    protected override void InstallNetwork(HardwareInfo info, SshClient sshClient, ScpClient scpClient)
    {
        if (!tokenSource.IsCancellationRequested)

            InstallRemote(info, sshClient, scpClient, progress);
    }


    internal void InstallRemote(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        IProgress<string> progress)
    {
        progress.Report("");
        progress.Report($"Adding network Firmware");

        var firmwareData = new Esp32Handler().GetFirmwareData(info.PlatformFamily, info.PlatformQualifier);

        if (firmwareData == null)
            throw new Exception("ESP Firmware Package (firmware.zip) is missing from the resources directory");

        if (firmwareData != null)
        {
            var zipFileTargetDirectory = $"/tmp/{Guid.NewGuid()}";
            var zipFileTargetPath = $"{zipFileTargetDirectory}/firmware.zip";
            var commandResult = sshClient.RunCommand($"mkdir -p {zipFileTargetDirectory}");

            if (commandResult.Result != string.Empty)
                throw new Exception(commandResult.Result);

            using MemoryStream data = new(firmwareData);

            scpClient.Upload(data, zipFileTargetPath);
            progress.Report($"   100% Uploaded");

            progress.Report("");
            progress.Report($"Unziping Firmware on Target");
            var unzipInstall = sshClient.RunCommand($"unzip {zipFileTargetPath} -d {zipFileTargetDirectory}");
            progress.Report(unzipInstall.Result);

            progress.Report($"Removing Temp File");
            var deleteInstallFile = sshClient.RunCommand($"rm {zipFileTargetPath}");

            if (deleteInstallFile.Error != string.Empty)
                throw new Exception(deleteInstallFile.Result);

            progress.Report($"Granting Execute to Install Script");
            commandResult = sshClient.RunCommand($"chmod +x {zipFileTargetDirectory}/ESPFirmware/esp32_init.sh");

            progress.Report($"Installing Network Firmware");
            commandResult = sshClient.RunCommand($"{zipFileTargetDirectory}/ESPFirmware/esp32_init.sh {zipFileTargetDirectory}");
            if (commandResult.Error != string.Empty)
                throw new Exception(commandResult.Error);

            commandResult = sshClient.RunCommand($"rm -r {zipFileTargetDirectory}");
            if (commandResult.Result != string.Empty)
                throw new Exception(commandResult.Result);
        }
    }
}
