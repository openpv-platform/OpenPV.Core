using Ahsoka.Utility;
using Mono.Unix.Native;
using NuGet.Protocol.Plugins;
using ShimSkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace Ahsoka.DeveloperTools;

internal class DotNetSDKTool : OpenPVToolBase
{
    bool isDownloaded = false;

    protected override bool GetIsDownloaded()
    {
        return isDownloaded;
    }

    public DotNetSDKTool()
    {
        this.PackageType = "Software Install";
        this.CanInstall = true;
        this.InstallText = "This process will install the DotNet SDK Needed for Optimized Service Managers.";
        this.IncludedProfiles = PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;
        this.Description = "Service Manager Compile Tools (DotNet SDK)";
        this.Resources = new();
        if (OperatingSystem.IsWindows())
            this.Resources.Add(new Resource() { DownloadPath = "https://dot.net/v1/dotnet-install.ps1" });
        else
            this.Resources.Add(new Resource() { DownloadPath = "https://dot.net/v1/dotnet-install.sh" });

        ProcessUtility.RunProcess("dotnet", "--list-sdks", null, out string result, out string error);

        if (result != null)
        {
             var lines = Regex.Split(result, "\r\n|\r|\n");
            foreach(var item in lines)
            {
                if (item.StartsWith("8."))
                {
                    string versionTemp = item.Substring(0, item.IndexOf("["));
                    if (String.Compare(versionTemp,this.Version) > 0)
                        this.Version = versionTemp;

                    isDownloaded = true;
                }
            }
        }

    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        await UpdateSDK(toolVM);
    }

    private async Task UpdateSDK(IToolViewModel toolVM)
    {

        toolVM.MainViewModel.Console.ClearConsole();

        toolVM.MainViewModel.StartProcess();

        string downloadFileName = LocalPath;
        string downloadDirectory = Path.GetDirectoryName(downloadFileName);

        await Task.Run(async () =>
        {
            try
            {
                IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
                {
                    toolVM.MainViewModel.Console.AddConsoleLine(info.Data);
                });
                toolVM.MainViewModel.Console.AddConsoleLine($"Running SDK Install Script");
                toolVM.MainViewModel.Console.AddConsoleLine($" ");

                if (OperatingSystem.IsWindows())
                    ProcessUtility.RunProcessAsync("powershell.exe", $"-NoProfile -ExecutionPolicy ByPass -File \"dotnet-install.ps1\" --version 8.0.303 ", LocalPath, progressText, CancellationToken.None);
               else
                    ProcessUtility.RunProcessAsync("dotnet-install.sh", $" --version 8.0.303 ", LocalPath, progressText, CancellationToken.None);

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }).ContinueWith(result =>
        {
            if (result.Result == "")
                toolVM.MainViewModel.Console.AddConsoleLine($"SDK Installed");
            else
                toolVM.MainViewModel.Console.AddConsoleLine($"Installation failed with error: {result.Result}");

            toolVM.MainViewModel.FinishProcess(result.Result, result.Result == "");
        });
    }
   
}
