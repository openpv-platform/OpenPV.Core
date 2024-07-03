using Ahsoka.Utility;
using NuGet.Protocol.Plugins;
using Renci.SshNet;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SkiaSharp.HarfBuzz.SKShaper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ahsoka.DeveloperTools;

internal class OpenViewSDKContainerTool : DockerBaseTool
{
    public OpenViewSDKContainerTool() :
        base("openpv/openview_sdk_3_0", "Ahsoka.Platform.3.0", true)
    {
        this.IncludedProfiles = PluginsViewModel.profileCPP;
        this.PackageType = "Software Install";
        this.CanLaunch = true;
        this.CanInstall = false;
        this.InstallText = "This process will download the 3.0 OpenView Select SDK Container to your Operating System (Requires Docker)";
    }

    protected override string GetLaunchScript(string imageName, string containerName, string workingDirectoryPath)
    {
        return Properties.Resources.docker_run_shell.Replace("$IMAGE_NAME", imageName)
                                                 .Replace("$CONTAINER_NAME", containerName)
                                                 .Replace("$LOCAL_PATH", workingDirectoryPath)
                                                 .Replace("$ENTRY_POINT", "/bin/bash");
    }

    public override async Task LaunchTool(IToolViewModel toolVM)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!CheckDependencies(toolVM)) return;          

            var path = await toolVM.MainViewModel.FolderLocatorAsync("Please select your development folder");
            if (!Directory.Exists(path)) return;

            var devPath = Path.Combine(path, ".devcontainer");
            if (!Directory.Exists(devPath))
            {
                Directory.CreateDirectory(devPath);
                File.WriteAllText(Path.Combine(devPath, "devcontainer.json"), Properties.Resources.devcontainer);
                File.WriteAllText(Path.Combine(devPath, "CMakePresets.json"), Properties.Resources.CMakePresets);
            }


            await Task.Run(() =>
            {
                var process = ProcessUtility.CreateProcess("cmd.exe", $"/c devcontainer open {path}");
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.Start();

            });
        }
        else
        {
            await base.LaunchTool(toolVM);
        }
    }

    protected override bool CheckDependencies(IToolViewModel toolVM)
    {
        if (!base.CheckDependencies(toolVM)) return false;

        StringBuilder builder = new();
        CancellationTokenSource source = new();
        var progress = new LocalProgress<CommandOutput>(info =>
        {
            builder.AppendLine(info.Data);
        });

        ProcessUtility.RunProcessAsync("cmd", $"/c code --help", null, progress, source.Token);
        var result = builder.ToString().Replace("\0", "");
        if (!result.Contains("Visual Studio Code"))
        {
            _ = toolVM.MainViewModel.Dialog.ShowDialog("Missing Dependency", $"Must install Visual Studio Code to launch container", "Ok");
            return false;
        }

        builder = new();
        ProcessUtility.RunProcessAsync("cmd", $"/c devcontainer --help", null, progress, source.Token);
        result = builder.ToString().Replace("\0", "");
        if (!result.Contains("VS Code"))
        {
            _ = toolVM.MainViewModel.Dialog.ShowDialog("Missing Dependency", $"Must install devcontainer CLI in Visual Studio Code to launch container. ", "Ok");
            return false;
        }

        return true;
    }
}