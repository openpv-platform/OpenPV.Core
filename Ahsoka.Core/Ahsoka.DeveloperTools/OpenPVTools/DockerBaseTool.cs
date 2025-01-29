using Ahsoka.Utility;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal abstract class DockerBaseTool : OpenPVToolBase
{
    bool isDownloaded = false;
    readonly string containerName = string.Empty;
    string version = string.Empty;
    readonly bool isShell = false;

    protected string ImageName { get; set; }

    public DockerBaseTool(string imageName, string containerName, bool isShell)
    {
        this.ImageName = imageName;
        this.containerName = containerName;
        this.isShell = isShell;

        this.PackageType = "Software Install";
        this.CanLaunch = true;
        this.CanInstall = false;
        this.InstallText = "This process will download the Container to your Operating System (Requires Docker)";
    }

    public override string Version
    {
        get
        {
            return base.Version;
        }
        set
        {
            base.Version = value;
            UpdateVersion();
            OnPropertyChanged();
        }
    }


    private void UpdateVersion()
    {
        ProcessUtility.RunProcess("docker", $"images --filter=reference={ImageName}:{Version} --format \"{{{{.ID}}}}\"", "", out string result, out string error);
        isDownloaded = !String.IsNullOrEmpty(result) && String.IsNullOrEmpty(error);
        OnPropertyChanged(nameof(IsDownloaded));
    }

    protected override bool GetIsDownloaded()
    {
        return isDownloaded;
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource token)
    {
        toolVM.MainViewModel.Console.ClearConsole();
        toolVM.MainViewModel.StartProcess();

        string downloadFileName = LocalPath;
        string downloadDirectory = Path.GetDirectoryName(downloadFileName);

        await Task.Run(() =>
        {
            try
            {
                toolVM.MainViewModel.Console.AddConsoleLine($"Downloading  {ImageName}:{Version}");

                IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
                {
                    if (info.Data != null)
                        toolVM.MainViewModel.Console.AddConsoleLine(info.Data);
                });

                ProcessUtility.RunProcessAsync("docker", $"pull {ImageName}:{Version}", "", progressText, token.Token);

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    if (result.Result == "")
                        toolVM.MainViewModel.Console.AddConsoleLine($"Image Downloaded");
                    else
                        toolVM.MainViewModel.Console.AddConsoleLine($"Download Failed with Error error: {result.Result}");

                    UpdateVersion();
                }
                catch (Exception ex)
                {
                    toolVM.MainViewModel.Console.AddConsoleLine(ex.Message);
                }

                toolVM.MainViewModel.FinishProcess(result.Result, result.Result == "");
            });
        });

    }

    protected override void OnRemove(string path)
    {
        ProcessUtility.RunProcess("docker", $"image rm -f {ImageName}:{Version}", "", out string _, out string _);
        ProcessUtility.RunProcess("docker", $"container rm -f {containerName}", "", out _, out _);
        isDownloaded = false;
        version = null;
        OnPropertyChanged(nameof(Version));
    }

    public override async Task LaunchTool(IToolViewModel toolVM)
    {
        if (!CheckDependencies(toolVM)) return;

        ProcessUtility.RunProcess("wsl", $"docker ps --all --filter name={containerName}", null, out string result, out string error);
        bool containerCreated = result.Contains($"{ImageName}:{Version}");

        Process process = null;
        if (containerCreated)
            process = ProcessUtility.CreateProcess("wsl", $"docker start {containerName} --attach --interactive");
        else
        {
            var path = await toolVM.MainViewModel.FolderLocatorAsync("Please select your development folder");
            if (Directory.Exists(path))
                process = ProcessUtility.CreateProcess("wsl", GetLaunchCommand($"{ImageName}:{Version}", containerName, path));
        }


        if (process != null)
            await Task.Run(() =>
            {
                if (!isShell)
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                if (isShell) { _ = toolVM.MainViewModel.Dialog.ShowDialog("Launch Ready", $"Launching container {containerName}.  \r\nYour development folder is mounted at \"/projects\".", "Ok").GetAwaiter().GetResult(); }

                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.Start();

                if (!isShell) { _ = toolVM.MainViewModel.Dialog.ShowDialog("Launch Complete", "Your application should launch in a few seconds", "Ok"); return; }


            });
    }

    protected virtual bool CheckDependencies(IToolViewModel toolVM)
    {
        string result, error;
        if (OperatingSystem.IsWindows())
        {
            ProcessUtility.RunProcess("wsl", $"--list", null, out result, out error);

            result = result.Replace("\0", "");
            if (!result.Contains("Default") || !result.Contains("Ubuntu-"))
            {
                _ = toolVM.MainViewModel.Dialog.ShowDialog("Missing Dependency", $"Must install WSL to launch container, Please set an Ubuntu WSL Distro as the default", "Ok");
                return false;
            }
        }

        if (OperatingSystem.IsWindows())
            ProcessUtility.RunProcess("wsl", $"docker info", null, out result, out error);
        else
            ProcessUtility.RunProcess("docker", $"info", null, out result, out error);
        if (!result.Contains("Version:"))
        {
            _ = toolVM.MainViewModel.Dialog.ShowDialog("Missing Dependency", $"Must install Docker Desktop and enable WSL2 backend in settings", "Ok");
            return false;
        }

        if (OperatingSystem.IsWindows())
            ProcessUtility.RunProcess("wsl", $"apt list --installed | grep x11-xserver-utils", null, out result, out error);
        else
            ProcessUtility.RunProcess("/bin/sh", $"-c apt list --installed | grep x11-xserver-utils", null, out result, out error);
        if (!result.Contains("x11-xserver-utils"))
        {
            _ = toolVM.MainViewModel.Dialog.ShowDialog("Missing Dependency", $"Must install apt package x11-xserver-utils to enable xhost", "Ok");
            return false;
        }

        return true;
    }

    protected string GetLaunchCommand(string imageName, string containerName, string workingDirectoryPath)
    {
        // Translate Path
        workingDirectoryPath = TranslateToWSL(workingDirectoryPath);

        // Build docker Start Script for WSL
        string tempFile = GetLaunchScript(imageName, containerName, workingDirectoryPath);

        // Start Script
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"launch_{containerName}.sh");
        File.WriteAllText(tempFilePath, tempFile);

        return TranslateToWSL(tempFilePath);
    }

    protected abstract string GetLaunchScript(string imageName, string containerName, string pworkingDirectoryPathath);

    private static string TranslateToWSL(string path)
    {
        string tempPath = path.Replace(@"\", @"\\");
        ProcessUtility.RunProcess("wsl", $"wslpath -a {tempPath}", null, out string result, out string error);
        if (string.IsNullOrEmpty(error))
            return result.Trim();

        return path;
    }
}
