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
        
        // Temp Fixes for Platform
        string appProfile = Encoding.UTF8.GetString(Properties.Resources.LinuxAppProfile).Replace("wayland-1", "/run/wayland-0");
        scpClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes(appProfile)), "/etc/profile.d/app_profile.sh");

        // Add Fake MAC Address to prevent random generation.

        // Set Hostname



        return commandResult;
    }
}
