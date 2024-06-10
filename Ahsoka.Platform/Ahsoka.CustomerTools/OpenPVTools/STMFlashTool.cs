using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;
internal class STMFlashTool : OpenPVToolBase
{
    public STMFlashTool()
    {
        this.PackageType = "Software Install";
        this.CanInstall = true;
        this.InstallText = "This process will install the STM DFU Firmware Flash Tools on your local OS to support installing Firmware Packages.";
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        string pathToScript, zipPath, executable, args;
        bool admin;

        if (OperatingSystem.IsLinux())
        {
            pathToScript = Path.Combine(LocalPath, "stm_installer_linux.zip");
            zipPath = Path.Combine(LocalPath, "TempExtract");
            ZipFile.ExtractToDirectory(pathToScript, zipPath, true);
            List<string> file = Directory.GetFiles(zipPath, "*.linux", SearchOption.AllDirectories).ToList();
            executable = file.First();
            args = "";
            admin = false;
        }
        else if (OperatingSystem.IsMacOS())
        {
            pathToScript = Path.Combine(LocalPath, "stm_installer_mac.zip");
            zipPath = Path.Combine(LocalPath, "TempExtract");
            ZipFile.ExtractToDirectory(pathToScript, zipPath, true);
            List<string> file = Directory.GetFiles(zipPath, "SetupSTM32CubeProgrammer-2_14_0_macos", SearchOption.AllDirectories).ToList();
            executable = file.First();
            args = "";
            admin = false;
        }
        else if (OperatingSystem.IsWindows())
        {
            pathToScript = Path.Combine(LocalPath, "stm_installer_win64.zip");
            zipPath = Path.Combine(LocalPath, "TempExtract");
            ZipFile.ExtractToDirectory(pathToScript, zipPath, true);
            List<string> file = Directory.GetFiles(zipPath, "*.exe", SearchOption.AllDirectories).ToList();
            executable = file.First();
            args = "";
            admin = true;
        }
        else
            return;

        var process = ProcessUtility.CreateProcessInWindow(executable, args);
        if (admin)
            process.StartInfo.Verb = "runas";

        process.Start();

        await process.WaitForExitAsync(); // Wait for Install to Complete

        await SelectSTMToolExecutable(toolVM);

        Directory.Delete(zipPath, true);
    }

    internal async Task SelectSTMToolExecutable(IToolViewModel toolVM)
    {
        if (CheckDefaultPath(toolVM)) return;

        bool selecting = true;
        while (selecting)
        {
            var path = await toolVM.MainViewModel.FileLocatorAsync("Select STM32CubeProgrammer Directory", "", "Executable");
            if (path == null)
                selecting = await toolVM.MainViewModel.Dialog.ShowDialog("No directory selected", "Try again", "Cancel");
            else if (path.Contains("STM32CubeProgrammer.app"))
            {
                toolVM.MainViewModel.Settings.STMExecutable = Path.Combine(path,"Contents","MacOs","bin","STM32_Programmer_CLI");
                toolVM.MainViewModel.SaveSettings();
                return;
            }
            else if (path.Contains("STM32_Programmer_CLI") )
            {
                toolVM.MainViewModel.Settings.STMExecutable = path;
                toolVM.MainViewModel.SaveSettings();
                ConfigureLinuxPermissions(path);
                return;
            }
            else
                selecting = await toolVM.MainViewModel.Dialog.ShowDialog("STM32CubeProgrammer directory was not selected", "Try again", "Cancel");
        }
    }

    internal static bool CheckDefaultPath(IToolViewModel toolVM)
    {
        string executablePath = GetDefaultPath();

        if (File.Exists(executablePath))
        {
            toolVM.MainViewModel.Settings.STMExecutable = executablePath;
            toolVM.MainViewModel.SaveSettings();
            ConfigureLinuxPermissions(executablePath);
            return true;
        }
        return false;
    }

    internal static string GetDefaultPath()
    {
        var programmerName = "STM32_Programmer_CLI";
        string programmerPath;
        if (OperatingSystem.IsLinux())
        {
            programmerPath = Environment.ExpandEnvironmentVariables("%HOME%/STMicroelectronics/STM32Cube/STM32CubeProgrammer");
        }
        else if (OperatingSystem.IsMacOS())
        {
            programmerPath = "/Applications/STMicroelectronics/STM32Cube/STM32CubeProgrammer";
            if (!Directory.Exists(programmerPath))
                programmerPath = Environment.ExpandEnvironmentVariables("%HOME%/Applications/STMicroelectronics/STM32Cube/STM32CubeProgrammer");

            return Path.Combine(programmerPath, "STM32CubeProgrammer.app", "Contents", "MacOs", "bin", "STM32_Programmer_CLI");
        }
        else
        {
            programmerName += ".exe";
            programmerPath = Environment.ExpandEnvironmentVariables(@"C:\Program Files\STMicroelectronics\STM32Cube\STM32CubeProgrammer");
        }

        return Path.Combine(programmerPath, "bin", programmerName);
    }

    internal static void ConfigureLinuxPermissions(string executablePath)
    {
        if (OperatingSystem.IsLinux())
        {

            string userName = Environment.GetEnvironmentVariable("USER");

            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, $"sudo bash -c \"echo '{userName} ALL=(ALL) NOPASSWD: {executablePath}' > /etc/sudoers.d/99_sudo_open_pv_{userName}\"");

            using Process pProcess = new();
            pProcess.StartInfo.FileName = "/bin/bash";
            pProcess.StartInfo.Arguments = $"-c \"xterm -fs 14 -fa DejaVuSansMono -e 'bash {tempFile}'";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.Start();
            pProcess.WaitForExit();
            File.Delete(tempFile);
        }
    }
}
