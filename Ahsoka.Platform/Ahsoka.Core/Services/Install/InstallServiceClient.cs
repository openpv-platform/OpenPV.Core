using Ahsoka.Installer.Components;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Install;

/// <summary>
/// Client used to communicate with a running install process.
/// </summary>
public class InstallServiceClient : AhsokaClientBase<InstallMessageTypes.Ids>
{
    #region Fields
    IInstallerUI installerUI;
    #endregion

    #region Properties
    /// <summary>
    /// Instance of the IInstallerUI interface that will be notified as the install progresses
    /// </summary>
    public void SetInstallerUI(IInstallerUI installerUI) { this.installerUI = installerUI; }
    #endregion

    #region Methods
    /// <summary>
    /// Default Constructor using Default Service Configuration
    /// </summary>
    public InstallServiceClient()
        : base(ConfigurationLoader.GetServiceConfig(InstallService.Name), new InstallServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new InstallService(this.ServiceConfig);
    }

    /// <summary>
    /// Creates an install service and notifies it to begin the install process. 
    /// </summary>
    /// <param name="installFile">Path to the file to be installed (.opv format)</param>
    /// <param name="passCode">Passcode needed to validate the install prior to installation.</param>
    /// <param name="useLocalInstaller">Indicates that the install should run the onboard install service rather the 
    /// installer included in the .opv package file.</param>
    /// <returns></returns>
    public InstallResult StartServiceAndInstall(string installFile, string passCode, bool useLocalInstaller)
    {
        var serviceTask = Task.Run(() =>
        {
            // Launch Installer....we will use this executable to run the installer...only for testing
            if (useLocalInstaller)
            {
                // Start Async Service
                AhsokaRuntime.CreateBuilder()
                            .AddServices(new InstallService())
                            .StartLocalRuntime()
                            .WaitForShutdown();

            }
            else
            {
                // Unpack Installer so we can execute the Service In a Separate Process
                UnPackInstaller(installFile, out string installerPath, out string installerArgs);

                // Start the Service
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Path = {installerPath}, Args = {installerArgs}");
                RunServiceOutOfProcess(installerPath, installerArgs);// Blocks

                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Installer Process Finished.");

                // Remove Installer Folder
                Directory.Delete(Path.GetDirectoryName(installerPath), true);
            }
        });

        // Send Install Command to Service to Install
        var clientTask = Task.Run(() =>
        {
            return ExecuteInstall(installFile, passCode);
        });

        // Wait for Service Task to Complete
        serviceTask.Wait();

        InstallResult returnValue = InstallResult.UnknownError;

        // Wait for Client task to Complete
        // if it does not, end anyway
        if (clientTask.Wait(5000))
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Client Completed Successfully");

            installerUI?.InstallComplete(clientTask.Result);
            returnValue = clientTask.Result.Result;
        }
        else
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Installer Client Process Ended Abnormally");

            this.CurrentRuntime.RequestShutdown();
            this.CurrentRuntime.WaitForShutdown();

            returnValue = InstallResult.UnknownError;
            installerUI?.InstallComplete(new InstallCompleteInfo() { Result = InstallResult.UnknownError, Message = "The install process did not end successfully" });
        }

        // Remove opv Package
        File.Delete(installFile);

        return returnValue;
    }

    /// <inheritdoc/>
    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<InstallMessageTypes.Ids>())
        {
            case InstallMessageTypes.Ids.InstallProgress:
                installerUI?.InstallProgress((InstallProgressInfo)message);
                break;

            case InstallMessageTypes.Ids.InstallLog:
                installerUI?.Log((InstallLogInfo)message);
                break;
        }
    }

    private InstallCompleteInfo ExecuteInstall(string installFile, string passKey)
    {
        return SendNotificationWithResponse<InstallCompleteInfo>(InstallMessageTypes.Ids.InstallPackage, new InstallPackageRequest() { InstallPackagePath = installFile, InstallPassKey = passKey });
    }

    private void RunServiceOutOfProcess(string installerPath, string installerArgs)
    {
        // Give Process Execute Rights
        if (!OperatingSystem.IsWindows())
            Process.Start("chmod", "+x " + installerPath).WaitForExit();

        IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
        {
            try
            {
                if (!String.IsNullOrEmpty(info.Data))
                {
                    if (!info.Error)
                        installerUI?.Log(new InstallLogInfo() { LogMessage = info.Data, LogMessageType = LogMessageType.Information });
                    else
                        installerUI?.Log(new InstallLogInfo() { LogMessage = info.Data, LogMessageType = LogMessageType.Error });
                }
            }
            catch (Exception) { }

        });

        ProcessUtility.RunProcessAsync(installerPath, installerArgs, null, progressText, CancellationToken.None);
    }

    private void UnPackInstaller(string path, out string pathToInstallerFile, out string installerArguements)
    {
        string pathToInstaller = string.Empty;

        string temp = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.TempRoot);
        if (!Directory.Exists(temp))
            Directory.CreateDirectory(temp);

        // Open the File to Read Data
        using FileStream stream = new(path, FileMode.Open);
        // Get Installer Engine for Package
        InstallEngineBase installEngine = InstallEngineBase.GetInstallEngine(stream);

        // Extract InstallerPackage.zip from pxv1 to temp file
        string tempInstallerPackageFile = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName() + ".zip";
        installerUI?.InstallProgress(new InstallProgressInfo()
        {
            Message = "Extracting Installer..."
        });

        installerUI?.Log(new InstallLogInfo()
        {
            LogMessage = $"Extracting Installer to {tempInstallerPackageFile}"
        });

        using (FileStream installer = new(tempInstallerPackageFile, FileMode.Create))
            installEngine.ExtractPackage(CommandLineComponent.CommandLineComponentType, installer);


        // Extract InstallerPackage.zip to temp runtime directory
        string installerRunPath = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName();
        Directory.CreateDirectory(installerRunPath);
        if (Directory.Exists(installerRunPath))
            Directory.Delete(installerRunPath, true);

        installerUI?.InstallProgress(new InstallProgressInfo()
        {
            Message = "Inflating Installer..."
        });

        installerUI?.Log(new InstallLogInfo()
        {
            LogMessage = $"Inflating Installer to {installerRunPath}"
        });

        ZipFile.ExtractToDirectory(tempInstallerPackageFile, installerRunPath);


        // Delete temp file with InstallerPackage.zip
        File.Delete(tempInstallerPackageFile);

        // Path To Installer
        var attributes = installEngine.GetComponentAttributes(CommandLineComponent.CommandLineComponentType);
        pathToInstallerFile = Path.Combine(installerRunPath, attributes[CommandLineComponent.InstallerFileNameAttribute]);
        installerArguements = attributes[CommandLineComponent.InstallerArgsAttribute];

        installerUI?.Log(new InstallLogInfo()
        {
            LogMessage = $"Path: {pathToInstallerFile} Args:{installerArguements}"
        });
    }
    #endregion
}

