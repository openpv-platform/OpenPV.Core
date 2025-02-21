using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.ViewModel;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class OpenViewProFlashTool : OpenPVToolBase
{
    internal const string FolderName = "OpenViewPro_Flash_Tools";

    public OpenViewProFlashTool()
    {
        this.PackageType = "Software Install";
        this.CanInstall = true;
        this.InstallText = "This process will install the OpenViewPro DFU Driver on your local OS to support installing Firmware Packages.  You may be prompted for rights to install this driver.";
        this.IncludedProfiles = PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;

        this.LocalPath = CustomerToolPaths.GetToolPath(FolderName);
    }


    protected override Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource token)
    {
        return base.ExecuteDownload(toolVM, token).ContinueWith((r) =>
        {
            string fileName = Path.GetFileName(this.Resources.First().DownloadPath);
            string zipPath = Path.Combine(LocalPath, fileName);
            ZipFile.ExtractToDirectory(zipPath, LocalPath, true);
            File.Delete(zipPath);
        });
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        if (OperatingSystem.IsWindows())
        {
            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                toolVM.MainViewModel.Console.AddConsoleText(info.Data,false);
            });

            CancellationTokenSource token = new CancellationTokenSource();
            ProcessUtility.RunProcessAsync("cmd", "/c " + Path.Combine(LocalPath, "openviewpro-driver.exe"), LocalPath, progressText, token.Token);
        }
        else
            return;
    }
}