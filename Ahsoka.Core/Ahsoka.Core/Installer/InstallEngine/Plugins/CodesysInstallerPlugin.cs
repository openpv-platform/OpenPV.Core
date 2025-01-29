using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ahsoka.Installer.InstallEngine;

internal class CodesysInstallerPlugin : InstallerPlugin
{
    public override void CheckPrerequisites(PackageInformation packageInformation, IProgress<PackageProgressInfo> progress, CancellationToken token)
    {
        if (packageInformation.ApplicationType != ApplicationType.CODESYS)
            return;

        base.CheckPrerequisites(packageInformation, progress, token);

        if (packageInformation.ServiceInfo.RuntimeConfiguration.StartupDelay < 3)
        {
            progress.Report(new PackageProgressInfo() { Message = "CODESYS PreReqs: Overriding Startup Delay to 3s" });
            packageInformation.ServiceInfo.RuntimeConfiguration.StartupDelay = 3;
        }
        if (packageInformation.ServiceInfo.RuntimeConfiguration.ReleaseOption != System.StartOptions.AfterApplicationStart)
        {
            progress.Report(new PackageProgressInfo() { Message = "CODESYS PreReqs: Overriding Auto-Release to After App Start" });
            packageInformation.ServiceInfo.RuntimeConfiguration.ReleaseOption = System.StartOptions.AfterApplicationStart;
        }
        if (!packageInformation.Licenses.Any(x => x.LicenseName == "CODESYS Runtime")
            && packageInformation.PlatformFamily.ToString().StartsWith("OpenView"))
        {
            string error = $"Invalid Project Licenses.  In order to generate this package, you must select a Project and 'Include a CODESYS Runtime.'";
            throw new Exception(error);
        }
    }

    public override void PrepareApplicationArea(ref string buildLocation,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        if (info.ApplicationType != ApplicationType.CODESYS)
            return;

        // Create Temp Folder
        string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        // Search for App and CRC and copy to Output Folder
        string pathToApp = Path.Combine(buildLocation, info.ExecCommand);
        string appName = Path.GetFileNameWithoutExtension(info.ExecCommand);
        if (Path.Exists(pathToApp))
        {
            string appFolder = Path.Combine(tempFolder, appName);
            Directory.CreateDirectory(appFolder);
            File.Copy(Path.Combine(buildLocation, appName + ".app"), Path.Combine(appFolder, appName + ".app"), true);
            File.Copy(Path.Combine(buildLocation, appName + ".crc"), Path.Combine(appFolder, appName + ".crc"), true);
        }

        string pathToPlcOutputs = Path.Combine(buildLocation, "PlcLogic");
        if (Path.Exists(pathToPlcOutputs))
            FileUtilities.CopyDirectory(pathToPlcOutputs, tempFolder, true, null);

        // Modify the Build Folder to be our new Build Folder
        buildLocation = tempFolder;
    }

    public override void AddAdditionalApplicationFiles(string buildLocation,
        Dictionary<string, string> attributes,
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress)
    {
        if (info.ApplicationType != ApplicationType.CODESYS)
            return;

        // Write Pre-Helper.
        string uihelperPath = $"{ApplicationOutputComponent.ComponentTypeGuid}.start_ui_helper.sh";
        additionalFiles[uihelperPath] = UTF8Encoding.UTF8.GetBytes(Properties.Resources.CODESYS_PreStart);

        // Modify Start Script Working folder and Start Command
        attributes[StartupInstallerPlugin.OverrideWorkingFolder] = "/usr/bin/codesys";            //WorkingDirectory=/usr/bin/codesys
        attributes[StartupInstallerPlugin.OverrideApplicationName] = "codesyscontrol";                //ExecStart=/usr/bin/codesys/codesyscontrol
    }


    public override void CleanupApplication(string buildLocation, PackageInformation info, IProgress<PackageProgressInfo> progress)
    {
        if (info.ApplicationType != ApplicationType.CODESYS)
            return;

        base.CleanupApplication(buildLocation, info, progress);

        if (Directory.Exists(buildLocation))
            Directory.Delete(buildLocation, true);
    }

    public override void ModifyTargetInstall(string installDirectory, string applicationName)
    {
        // Read the CODESYSControl.cfg File adn Point to our new App.
        foreach (var file in Directory.EnumerateFiles("/usr/bin/codesys", "CODESYSControl*.cfg"))
            UpdateAppName(file, Path.GetFileNameWithoutExtension(applicationName));

        // Create Symlink to Current from /usr/bin/codesys/PlcLogic"
        // We will actually start from the codesys folder.
        if (Directory.Exists("/usr/bin/codesys/PlcLogic"))
            Directory.Delete("/usr/bin/codesys/PlcLogic", true);

        File.CreateSymbolicLink("/usr/bin/codesys/PlcLogic", installDirectory);

        // Fix for CAN
        ProcessUtility.RunProcessScript($"sed -i -r \"s/(BitRate=[0-9]*)/BitRate=250000/g\" /etc/systemd/network/80-can0.network", null, out string stdOut, out string stdErr);
        ProcessUtility.RunProcessScript($"sed -i -r \"s/(BitRate=[0-9]*)/BitRate=250000/g\" /etc/systemd/network/80-can1.network", null, out stdOut, out stdErr);

        // Add SyncJumpWidth to Network Setup - This (sjw=4) fixes devices who's baud rate clock is not within tolerance of our clock.
        ProcessUtility.RunProcessScript($"grep -qxF 'SyncJumpWidth=4' /etc/systemd/network/80-can0.network || echo 'SyncJumpWidth=4' >> /etc/systemd/network/80-can0.network", null, out stdOut, out stdErr);
        ProcessUtility.RunProcessScript($"grep -qxF 'SyncJumpWidth=4' /etc/systemd/network/80-can1.network || echo 'SyncJumpWidth=4' >> /etc/systemd/network/80-can1.network", null, out stdOut, out stdErr);
    }

    private static void UpdateAppName(string file, string appName)
    {
        string backupFile = file + ".orig";

        var configFile = File.ReadAllLines(file, Encoding.UTF8).ToList();

        // Backup Original
        if (!File.Exists(backupFile))
            File.WriteAllText(backupFile, string.Join("\n", configFile));

        bool foundStart = false;
        for (int i = 0; i < configFile.Count; i++)
        {
            string line = configFile[i];

            if (line.StartsWith("[CmpApp]"))
            {
                foundStart = true;
                continue;
            }

            // Found App, Replace it
            if (foundStart && line.StartsWith("Application.1="))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Updating CODESYS AppName to '{appName}' in {file}");
                configFile[i] = $"Application.1={appName}";
                break;
            }

            // Remove Extra Lines
            if (foundStart && line.Trim().Length == 0)
            {
                configFile.RemoveAt(i);
                i -= 1;
                continue;
            }

            // Found Next StartTag, Add App Before This
            if (foundStart && line.StartsWith("["))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Updating CODESYS AppName to '{appName}' in {file}");
                configFile.Insert(i, $"Application.1={appName}");
                configFile.Insert(i + 1, " ");
                break;
            }
        }

        File.WriteAllText(file, string.Join("\n", configFile));

    }

}