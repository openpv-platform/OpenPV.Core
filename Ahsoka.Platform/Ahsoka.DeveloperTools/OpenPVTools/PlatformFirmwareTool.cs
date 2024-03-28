using Ahsoka.DeveloperTools.ViewModel;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;


internal class PlatformFirmwareTool : SparkToolBase
{
    readonly ReleaseDTO viewModel;

    public PlatformFirmwareTool(ReleaseDTO viewModel, ReleaseFileDTO item) : base(viewModel, false)
    {
        this.PackageType = "Target Firmware";
        this.Description = "Distribution for " + viewModel.ContainerName;
        this.CanInstall = true;
        this.viewModel = viewModel;
        this.Version = viewModel.ContainerVersion;
        this.Name = viewModel.ReleaseName + " (Firmware)";
        this.CanLaunch = false;
        InstallText = "To install firmware, first put the target into DFU mode using a magnet, then hit continue to begin the update.  Note that this process requires that you have downloaded the appropriate flash tools.";

        this.LocalPath = Path.Combine(CustomerToolPaths.GetToolPath("Firmware"), item.Path);
    }

    protected override async Task ExecuteDownload(IToolViewModel toolModel, CancellationTokenSource cancellationToken)
    {
        await ExecuteDownloadInternal(toolModel, LocalPath, cancellationToken);
    }

    public override async Task InstallTool(IToolViewModel toolModel)
    {
        string connection = ConnectToTarget(toolModel);
        if (!connection.Contains("Connected with SN:"))
        {
            _ = toolModel.MainViewModel.Dialog.ShowDialog("Device not Found", connection, "Ok");
            throw new ApplicationException("Device Not Found");
        }

        toolModel.MainViewModel.Console.ClearConsole();

        toolModel.MainViewModel.StartProcess();

        string downloadFileName = LocalPath;
        string downloadDirectory = Path.GetDirectoryName(downloadFileName);

        await Task.Run(async () =>
        {
            try
            {
                toolModel.MainViewModel.Console.AddConsoleLine($"Beginning Installation of {viewModel.Contents.First().Path}");

                toolModel.MainViewModel.Console.AddConsoleLine($"Extracting files from zipped folder");

                string zipPath = Path.Combine(downloadDirectory, "TempExtract");

                ZipFile.ExtractToDirectory(downloadFileName, zipPath, true);
                toolModel.MainViewModel.Console.AddConsoleLine($"Extraction Complete");

                List<string> file = Directory.GetFiles(zipPath, "*.tsv", SearchOption.AllDirectories).ToList();
                string installFile = file.First();

                string result = "";
                if (viewModel.ContainerName == "OpenViewLinux")
                    result = InstallOnTargetLinux(installFile, toolModel);

                Directory.Delete(zipPath, true);
                if (result == "")
                    return result;

                bool ready = await toolModel.MainViewModel.Dialog.ShowDialog("Continue", "Please restart display then press continue", "Continue");

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }).ContinueWith(result =>
        {
            if (result.Result == "")
                toolModel.MainViewModel.Console.AddConsoleLine($"Image Installed and device ready for Prep");
            else
                toolModel.MainViewModel.Console.AddConsoleLine($"Installation failed with error: {result.Result}");

            toolModel.MainViewModel.FinishProcess(result.Result, result.Result == "");
        });
    }

    internal string ConnectToTarget(IToolViewModel toolVM)
    {
        string result = "";
        if (viewModel.ContainerName == "OpenViewLinux")
            result = ConnectToTargetLinux(toolVM);

        return result;
    }

    internal string ConnectToTargetLinux(IToolViewModel toolVM)
    {
        string pathToCli = GetExecutablePath(toolVM);
        if (!File.Exists(pathToCli))
            return "Firmware Installer Path not found...you may need to reinstall the ST Firmware Installer.";

        string result = "";
        toolVM.MainViewModel.Console.ClearConsole();
        using (Process pProcess = new())
        {
            if (OperatingSystem.IsLinux() )
            {
                pProcess.StartInfo.FileName = "/bin/bash";
                pProcess.StartInfo.Arguments = $"-c \"sudo {pathToCli} -l usb\"";
            }
            else if ( OperatingSystem.IsMacOS())
            {
                pProcess.StartInfo.FileName = pathToCli;
                pProcess.StartInfo.Arguments = $"-l usb";
            }
            else
            {
                pProcess.StartInfo.FileName = pathToCli;
                pProcess.StartInfo.Arguments = "-l usb";
            }

            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
            pProcess.Start();
            while (!pProcess.StandardOutput.EndOfStream)
            {
                var line = pProcess.StandardOutput.ReadLine();
                toolVM.MainViewModel.Console.AddConsoleLine(line);
                if (line.ToLower().Contains("No STM32 device".ToLower()))
                    result = "No device was found connected in DFU Mode.  To start in DFU Mode, align a magnet with the mark on the back of the display and cycle power on the display.";
                if (line.ToLower().Contains("Serial Number".ToLower()))
                    result = $"Connected with SN: {line.Substring(25)}";
            }
            pProcess.WaitForExit();
        }
        return result;
    }

    private static string GetExecutablePath(IToolViewModel toolVM)
    {
        string path = toolVM.MainViewModel.Settings.STMExecutable;
        if (!File.Exists(path))
            return STMFlashTool.GetDefaultPath();

        return path;
    }

    internal string InstallOnTargetLinux(string installFile, IToolViewModel toolVM)
    {
        string pathToCli = GetExecutablePath(toolVM);
        if (!File.Exists(pathToCli))
            return "Firmware Installer not found...you may need to reinstall the ST Firmware Installer.";

        using Process pProcess = new();
        if (OperatingSystem.IsLinux())
        {
            pProcess.StartInfo.FileName = "/bin/bash";
            pProcess.StartInfo.Arguments = $"-c \"sudo {pathToCli} -c port=USB1 -w {installFile}\"";
        }
        else if (OperatingSystem.IsMacOS())
        {
            pProcess.StartInfo.FileName = pathToCli;
            pProcess.StartInfo.Arguments = $"-c port=USB1 -w {installFile}\"";
        }
        else
        {
            pProcess.StartInfo.FileName = pathToCli;
            pProcess.StartInfo.Arguments = $"-c port=USB1 -w {installFile}";
        }

        pProcess.StartInfo.UseShellExecute = false;
        pProcess.StartInfo.RedirectStandardOutput = true;
        pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
        pProcess.Start();
        bool print = false;
        while (!pProcess.StandardOutput.EndOfStream)
        {
            int character = (char)pProcess.StandardOutput.Read();

            if (character == 27)
                print = false; // Disable Printing
            else if (!print)
            {
                if (character == 'm')
                    print = true;
            }
            else if (character == 219)
                toolVM.MainViewModel.Console.TrimLastLine();
            else if (character > 128) { } // Eat These
            else if (character is >= 32 or 10 or 13)
                toolVM.MainViewModel.Console.AddConsoleCharacter((char)character);
        }
        pProcess.WaitForExit();
        return "";
    }


}
