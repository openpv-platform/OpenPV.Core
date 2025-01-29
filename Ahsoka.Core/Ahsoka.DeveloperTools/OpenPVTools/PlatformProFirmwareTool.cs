using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class PlatformProFirmwareTool : PlatformFirmwareTool
{
    string vidPid = "0451:6165";

    string pathToFirmware = string.Empty;
    PlatformFamily family = PlatformFamily.Any;

    public PlatformProFirmwareTool(string pathToFirmware, PlatformFamily family) : base(pathToFirmware, family)
    {
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
        string connection = ConnectToTarget(toolModel, out string pathToCli);
        if (!connection.Contains("Connected to DevKit"))
        {
            _ = toolModel.MainViewModel.Dialog.ShowDialog("Device not Found", connection, "Ok");
            throw new ApplicationException("Device Not Found");
        }

        //toolModel.MainViewModel.Console.ClearConsole();

        toolModel.MainViewModel.StartProcess();

        string downloadFileName = LocalPath;
        string downloadDirectory = Path.GetDirectoryName(downloadFileName);

        await Task.Run(async () =>
        {
            try
            {
                toolModel.MainViewModel.Console.AddConsoleLine($"Beginning Installation of {pathToFirmware}");

                toolModel.MainViewModel.Console.AddConsoleLine($"Extracting files from zipped folder");

                string firmwareFolder = Path.Combine(downloadDirectory, "TempExtract");

                ZipFile.ExtractToDirectory(downloadFileName, firmwareFolder, true);
                toolModel.MainViewModel.Console.AddConsoleLine($"Extraction Complete");

                string result = "";
                if (family == PlatformFamily.OpenViewLinuxPro)
                    result = ExecuteDFUInstall(firmwareFolder, toolModel, pathToCli);

                Directory.Delete(firmwareFolder, true);
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

    private string ExecuteDFUInstall(string firmwareDir, IToolViewModel toolModel, string dfuName)
    {

        toolModel.MainViewModel.Console.AddConsoleLine($"Beginning Firmware Load");
        toolModel.MainViewModel.Console.AddConsoleLine($" ");
        toolModel.MainViewModel.Console.AddConsoleLine($"Loading initial tiboot3.bin to R5 core RAM.");
        RunCommand(dfuName, $"-R -a bootloader -d {vidPid} -D ./dfu-tiboot3-j722s-hs-fs-evm-r5.bin -w", firmwareDir, toolModel);

        toolModel.MainViewModel.Console.AddConsoleLine($" ");
        toolModel.MainViewModel.Console.AddConsoleLine($"Loading A53 binaries to RAM.");
        RunCommand(dfuName, $"-R -a tispl.bin -d {vidPid} -D ./dfu-tispl.bin -w", firmwareDir, toolModel);
        RunCommand(dfuName, $"-R -a u-boot.img -d {vidPid} -D ./dfu-u-boot.img -w", firmwareDir, toolModel);

        toolModel.MainViewModel.Console.AddConsoleLine($" ");
        toolModel.MainViewModel.Console.AddConsoleLine($"Loading tiboot3.bin to NOR");
        RunCommand(dfuName, $"-a tiboot3.bin -d {vidPid} -D ./tiboot3.bin -w", firmwareDir, toolModel);

        toolModel.MainViewModel.Console.AddConsoleLine($" ");
        toolModel.MainViewModel.Console.AddConsoleLine($"Loading fitImage to NOR");
        RunCommand(dfuName, $"-R -a tispl.bin -d {vidPid} -D ./fitImage -w", firmwareDir, toolModel);

        toolModel.MainViewModel.Console.AddConsoleLine($" ");
        toolModel.MainViewModel.Console.AddConsoleLine($"Loading rootfs to EMMC");
        RunCommand(dfuName, $"-R -a rootfs -d {vidPid} -D ./hlio-image-openviewpro-base-j722s-evm.rootfs.ext4 -w", firmwareDir, toolModel);

        return "Firmware Installed";
    }

    private string ConnectToTarget(IToolViewModel toolModel, out string pathToCli)
    {
        pathToCli = CustomerToolPaths.GetToolPath("DeveloperTools");
        pathToCli = Path.Combine(pathToCli, "OpenViewPro_Flash_Tools", "utils");

        if (!Directory.Exists(pathToCli))
            return "Firmware Installer Path not found...you may need to reinstall OpenViewPro tools";

        string result = "";
        toolModel.MainViewModel.Console.ClearConsole();
        using (Process pProcess = new())
        {
            if (OperatingSystem.IsLinux())
            {
                pathToCli = Path.Combine(pathToCli, "linux-amd64", "dfu-util"); //TODO Linux.
                pProcess.StartInfo.FileName = "/bin/bash";
                pProcess.StartInfo.Arguments = $"-c \"sudo {pathToCli} -l \"";
            }
            else if (OperatingSystem.IsMacOS())
            {
                pathToCli = Path.Combine(pathToCli, "darwin-x86_64", "dfu-util.exe"); //TODO Linux.

                pProcess.StartInfo.FileName = pathToCli;
                pProcess.StartInfo.Arguments = $"-l";
            }
            else
            {
                pathToCli = Path.Combine(pathToCli, "win64", "dfu-util.exe"); //TODO Linux.
                pProcess.StartInfo.FileName = pathToCli;
                pProcess.StartInfo.Arguments = "-l";
            }

            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
            pProcess.Start();
            while (!pProcess.StandardOutput.EndOfStream)
            {
                var line = pProcess.StandardOutput.ReadLine();
                toolModel.MainViewModel.Console.AddConsoleLine(line);
                if (line.ToLower().Contains($"found dfu: [{vidPid}]"))
                    result = $"Connected to DevKit";

            }
            pProcess.WaitForExit();
        }
        return result;
    }

    internal string RunCommand(string pathToCli, string commandToRun, string firmwareDir, IToolViewModel toolVM)
    {
        using (Process pProcess = new())
        {
            if (OperatingSystem.IsLinux())
            {
                pProcess.StartInfo.FileName = "/bin/bash";
                pProcess.StartInfo.Arguments = $"-c \"sudo {pathToCli} {commandToRun}\"";
            }
            else if (OperatingSystem.IsMacOS())
            {
                pProcess.StartInfo.FileName = pathToCli;
                pProcess.StartInfo.Arguments = $"-c {pathToCli} {commandToRun}";
            }
            else
            {
                pProcess.StartInfo.FileName = pathToCli;
                pProcess.StartInfo.Arguments = $"{commandToRun}";
            }

            pProcess.StartInfo.WorkingDirectory = firmwareDir;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
            pProcess.Start();

            bool waitingForLineFeed = false;
            while (!pProcess.StandardOutput.EndOfStream)
            {
                int character = (char)pProcess.StandardOutput.Read();
                if (character is >= 32 && character < 128)
                {
                    if (waitingForLineFeed)
                    {
                        toolVM.MainViewModel.Console.TrimLastLine();
                        waitingForLineFeed = false;
                    }

                    toolVM.MainViewModel.Console.AddConsoleCharacter((char)character);
                }
                else if (character is '\n')
                {
                    if (waitingForLineFeed)
                    {
                        toolVM.MainViewModel.Console.AddConsoleCharacter('\r');
                        toolVM.MainViewModel.Console.AddConsoleCharacter((char)character);
                        waitingForLineFeed = false;
                    }
                }
                else if (character is '\r')
                {
                    waitingForLineFeed = true;
                }
            }

            pProcess.WaitForExit();
        }

        return "";


    }



}
