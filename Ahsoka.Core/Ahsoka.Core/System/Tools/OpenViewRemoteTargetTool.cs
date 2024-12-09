#pragma warning disable CS1591
using Ahsoka.Services.Device.Handlers;
using Ahsoka.System.Hardware;
using Renci.SshNet;
using System;

namespace Ahsoka.System;


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
           new Esp32Handler()?.InstallRemote(info, sshClient, scpClient, progress);
    }
}
