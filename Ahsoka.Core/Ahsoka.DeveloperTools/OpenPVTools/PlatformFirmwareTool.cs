using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
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


internal class PlatformFirmwareTool : OpenPVToolBase
{
    string pathToFirmware = string.Empty;
    PlatformFamily family = PlatformFamily.Any;
    public PlatformFirmwareTool(string pathToFirmware, PlatformFamily family) 
    {

        pathToFirmware = Directory.EnumerateFiles(pathToFirmware,"image.*.zip", SearchOption.AllDirectories).FirstOrDefault();

        this.family = family;
        this.PackageType = "Target Firmware";
        this.Description = "Distribution for " + family;
        this.CanInstall = true;
        
        
        this.Name = Path.GetFileName(pathToFirmware) + " (Firmware)";
        this.CanLaunch = false;
        InstallText = "To install firmware, first put the target into DFU mode using a magnet, then hit continue to begin the update.  Note that this process requires that you have downloaded the appropriate flash tools.";

        if (!String.IsNullOrEmpty(pathToFirmware))
        {
            this.Version = Path.GetFileName(pathToFirmware).Replace("image.", "").Replace(".zip", "");
            string versionFile = Path.Combine(Path.GetDirectoryName(pathToFirmware), PlatformSupportTool.VersionFileName);
            if (!File.Exists(versionFile))
                File.WriteAllText(versionFile, this.Version);
        }

        this.LocalPath = pathToFirmware;
    }

    protected override async Task ExecuteDownload(IToolViewModel toolModel, CancellationTokenSource cancellationToken)
    {
        await Task.FromResult(0);
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
                toolModel.MainViewModel.Console.AddConsoleLine($"Beginning Installation of {pathToFirmware}");

                toolModel.MainViewModel.Console.AddConsoleLine($"Extracting files from zipped folder");

                string zipPath = Path.Combine(downloadDirectory, "TempExtract");

                ZipFile.ExtractToDirectory(downloadFileName, zipPath, true);
                toolModel.MainViewModel.Console.AddConsoleLine($"Extraction Complete");

                List<string> file = Directory.GetFiles(zipPath, "*.tsv", SearchOption.AllDirectories).ToList();
                string installFile = file.First();

                string result = ""; 
                if (family ==  PlatformFamily.OpenViewLinux)
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

    private string ConnectToTarget(IToolViewModel toolVM)
    {
        string result = "";
        if (family == PlatformFamily.OpenViewLinux)
            result = ConnectToTargetLinux(toolVM);

        return result;
    }

    private string ConnectToTargetLinux(IToolViewModel toolVM)
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

    private string GetExecutablePath(IToolViewModel toolVM)
    {
        string path = toolVM.MainViewModel.Settings.STMExecutable;
        if (!File.Exists(path))
            return STMFlashTool.GetDefaultPath();

        return path;
    }

    private string InstallOnTargetLinux(string installFile, IToolViewModel toolVM)
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
