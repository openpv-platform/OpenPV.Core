 #pragma warning disable CS1591
using Ahsoka.Installer.InstallEngine;
using Ahsoka.System.Hardware;
using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
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
        string appProfile = Encoding.UTF8.GetString(Properties.Resources.LinuxAppProfile);
        scpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes(appProfile)), "/etc/profile.d/app_profile.sh");

        progress.Report($"Remove Unused Services");
        sshClient.RunCommand($"systemctl disable containerd");
        sshClient.RunCommand($"systemctl disable docker.socket");
        sshClient.RunCommand($"systemctl disable demo");
        sshClient.RunCommand($"systemctl disable snmpd");
        sshClient.RunCommand($"systemctl disable gplv3-notice");
        sshClient.RunCommand($"systemctl disable systemd-bootchart.service");
        sshClient.RunCommand($"systemctl mask systemd-udev-settle");
        var result = sshClient.RunCommand($"systemctl disable tee-supplicant");

        result = sshClient.RunCommand($"mkdir -p /usr/local/Ahsoka/factory/weston");

        string iniFilePath = $"/usr/local/Ahsoka/factory/weston/weston.ini";
        using (var memoryStream = new MemoryStream())
        {
            using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.weston_ini)))
                scpClient.Upload(uploadStream, iniFilePath);
        }

        // Temp Fixes for Platform
        string ahsokaEarly = Properties.Resources.hlio_early;
        scpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes(ahsokaEarly)), "/usr/sbin/hlio-init.sh");

        return null;
    }

    protected override SshCommand EnableServices(HardwareInfo info, SshClient sshClient)
    {
        SshCommand commandResult = base.EnableServices(info, sshClient);
        progress.Report($"Enable Layer Manager Service");
        commandResult = sshClient.RunCommand($"systemctl enable Ahsoka.LayerManager.service");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        return commandResult;
    }

    protected override SshCommand AddServices(HardwareInfo info, SshClient sshClient, ScpClient scpClient)
    {
        SshCommand commandResult = base.AddServices(info, sshClient, scpClient);

        progress.Report($"Install Ahsoka LayerManager Application");
        string path = Path.Combine(PlatformSupportPathInfo.GetPlatformAppsPath(info.PlatformFamily), "Ahsoka.LayerManager.OpenViewLinuxPro");

        string appPath = $"/usr/local/Ahsoka/factory/weston/Ahsoka.LayerManager";
        using (var memoryStream = new MemoryStream(File.ReadAllBytes(path)))
            scpClient.Upload(memoryStream, appPath);

        progress.Report($"Adding Ahsoka LayerManager Service");
        string unitFilePath = $"/etc/systemd/system/Ahsoka.LayerManager.service";
        using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.Ahsoka_LayerManager)))
            scpClient.Upload(memoryStream, unitFilePath);

        progress.Report($"Setting Permissions on LayerManager");
        commandResult = sshClient.RunCommand($"chmod 644 {unitFilePath}");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);

        progress.Report("");
        progress.Report($"Adding Layer Start Script Script");
        string startScriptPath = $"/usr/local/Ahsoka/factory/weston/layer_manager_start.sh";
        string startScriptFileContents = Properties.Resources.Ahsoka_LayerManagerStart;

        using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(startScriptFileContents)))
            scpClient.Upload(memoryStream, startScriptPath);

        progress.Report($"Granting Execute to Install Script");
        commandResult = sshClient.RunCommand($"chmod +x {startScriptPath}");
        if (commandResult.Result != string.Empty)
            throw new Exception(commandResult.Result);


        return commandResult;
    }
}
