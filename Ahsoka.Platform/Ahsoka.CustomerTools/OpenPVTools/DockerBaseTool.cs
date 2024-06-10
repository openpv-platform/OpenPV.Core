using Ahsoka.Utility;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

internal abstract class DockerBaseTool : OpenPVToolBase
{
    bool isDownloaded = false;
    readonly string imageName = string.Empty;
    readonly string containerName = string.Empty;
    string version = string.Empty;
    readonly bool isShell = false;

    public DockerBaseTool(string imageName, string containerName, bool isShell)
    {
        this.imageName = imageName;
        this.containerName = containerName;
        this.isShell = isShell;

        this.PackageType = "Software Install";
        this.CanLaunch = true;
        this.CanInstall = false;
        this.InstallText = "This process will download the Container to your Operating System (Requires Docker)";
        UpdateVersion(imageName);
    }

    private void UpdateVersion(string imageName)
    {
        ProcessUtility.RunProcess("docker", $"images --filter=reference={imageName} --format \"{{{{.ID}}}}\"", "", out string result, out string error);
        isDownloaded = !String.IsNullOrEmpty(result) && String.IsNullOrEmpty(error);
        if (isDownloaded)
            version = result.ToUpper().Trim();

        OnPropertyChanged(nameof(Version));
    }

    public override string Version { get => version; set { } }

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
                toolVM.MainViewModel.Console.AddConsoleLine($"Downloading  {imageName}");

                IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
                {
                    if (info.Data != null)
                        toolVM.MainViewModel.Console.AddConsoleLine(info.Data);
                });

                ProcessUtility.RunProcessAsync("docker", $"pull {imageName}", "", progressText, token.Token);

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

                    UpdateVersion(imageName);
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
        ProcessUtility.RunProcess("docker", $"image rm -f {imageName}", "", out string _, out string _);
        ProcessUtility.RunProcess("docker", $"container rm -f {containerName}", "", out _, out _);
        isDownloaded = false;
        version = null;
        OnPropertyChanged(nameof(Version));
    }

    public override async Task LaunchTool(IToolViewModel toolVM)
    {
        ProcessUtility.RunProcess("wsl", $"docker ps --all --filter name={containerName}", null, out string result, out string error);
        bool containerCreated = result.Contains(imageName);

        Process process = null;
        if (containerCreated)
            process = ProcessUtility.CreateProcess("wsl", $"docker start {containerName} --attach --interactive");
        else
        {
            var path = await toolVM.MainViewModel.FolderLocatorAsync("Please select your development folder");
            if (Directory.Exists(path))
                process = ProcessUtility.CreateProcess("wsl", GetLaunchCommand(imageName, containerName, path));
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
