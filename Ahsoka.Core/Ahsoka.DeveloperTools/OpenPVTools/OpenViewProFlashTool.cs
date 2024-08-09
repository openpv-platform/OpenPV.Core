using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
using Ahsoka.Utility;
using Avalonia.Controls;
using ShimSkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        return  base.ExecuteDownload(toolVM, token).ContinueWith((r) =>
        {
            string fileName = Path.GetFileName(this.Resources.First().DownloadPath);
            string zipPath = Path.Combine(LocalPath, fileName);
            ZipFile.ExtractToDirectory(zipPath, LocalPath, true);
            File.Delete(zipPath);
        });
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        string pathToScript = "", executable = "", args = "";
        bool admin = false;

        if (OperatingSystem.IsLinux())
        {
           /* pathToScript = Path.Combine(LocalPath, "stm_installer_linux.zip");
            zipPath = Path.Combine(LocalPath, "TempExtract");
            ZipFile.ExtractToDirectory(pathToScript, zipPath, true);
            List<string> file = Directory.GetFiles(zipPath, "*.linux", SearchOption.AllDirectories).ToList();
            executable = file.First();
            args = "";
            admin = false;*/
        }
        else if (OperatingSystem.IsMacOS())
        {
           /* pathToScript = Path.Combine(LocalPath, "stm_installer_mac.zip");
            zipPath = Path.Combine(LocalPath, "TempExtract");
            ZipFile.ExtractToDirectory(pathToScript, zipPath, true);
            List<string> file = Directory.GetFiles(zipPath, "SetupSTM32CubeProgrammer-2_14_0_macos", SearchOption.AllDirectories).ToList();
            executable = file.First();
            args = "";
            admin = false;*/
        }
        else if (OperatingSystem.IsWindows())
        {
            pathToScript = Path.Combine(LocalPath, "driver\\OpenViewPro_DFU.inf");
            executable = "C:\\Windows\\System32\\InfDefaultInstall.exe";
            args = pathToScript;

            admin = true;
        }
        else
            return;


        var process = ProcessUtility.CreateProcessInWindow(executable, args);
        if (admin)
            process.StartInfo.Verb = "runas";

        process.Start();

        await process.WaitForExitAsync(); // Wait for Install to Complete
        process.Dispose();
    }
}