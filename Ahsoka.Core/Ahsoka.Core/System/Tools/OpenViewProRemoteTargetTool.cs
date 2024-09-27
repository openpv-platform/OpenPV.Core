#pragma warning disable CS1591
using Ahsoka.Installer.InstallEngine;
using Ahsoka.System.Hardware;
using Renci.SshNet;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Ahsoka.System;

internal class OpenViewProRemoteTargetTool : LinuxRemoteTargetTool
{
    protected override bool IsCompatibleFirmware(HardwareInfo info, SshClient client, out bool isExactMatch, IProgress<string> progress)
    {
        isExactMatch = false; 
        return true;
    }

    // Platform Specific logic.
    [ExcludeFromCodeCoverage]
    protected override SshCommand InstallDependencies(HardwareInfo info,
                          SshClient sshClient, ScpClient scpClient)
    {
        
        // Temp Fixes for Platform
        string appProfile = Encoding.UTF8.GetString(Properties.Resources.LinuxAppProfile).Replace("wayland-1", "/run/wayland-0");
        scpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes(appProfile)), "/etc/profile.d/app_profile.sh");


        progress.Report($"Remove Unused Services");
        sshClient.RunCommand($"systemctl disable containerd");
        sshClient.RunCommand($"systemctl disable docker.socket");
        sshClient.RunCommand($"systemctl disable demo");
        sshClient.RunCommand($"systemctl disable snmpd");
        sshClient.RunCommand($"systemctl disable gplv3-notice");
        sshClient.RunCommand($"systemctl disable strongswan-starter");
        sshClient.RunCommand($"systemctl disable systemd-bootchart.service");
        var result = sshClient.RunCommand($"systemctl disable tee-supplicant");

        return result;
    }
}
