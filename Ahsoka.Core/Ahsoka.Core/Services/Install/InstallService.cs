#pragma warning disable CS1591
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.Install;

[AhsokaService(Name)]
public class InstallService : AhsokaServiceBase<InstallMessageTypes.Ids>
{
    public const string Name = "InstallService";
    InstallProgressInfo lastInfo;
    AhsokaServiceRequest clientRequest = null;

    public InstallService()
        : this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public InstallService(ServiceConfiguration config) :
        base(config, new InstallServiceMessages())
    {

    }

    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case InstallMessageTypes.Ids.InstallPackage:

                ExecuteInstall(request, request.Message as InstallPackageRequest);
                break;
        }
    }

    private void ExecuteInstall(AhsokaServiceRequest messageHeader, InstallPackageRequest installInfo)
    {
        // If the client header is not null another install must be in progress.
        if (clientRequest != null)
        {
            SendResponse(messageHeader, new InstallCompleteInfo() { Message = "Install in Progress", Result = InstallResult.PreviousInstallInProgress });
            return;
        }

        // Capture the client header so we can send responses back to the correct client
        this.clientRequest = messageHeader;

        InstallCompleteInfo result = null;

        // Stream will be closed after RunInstall is completed
        using (var stream = new FileStream(installInfo.InstallPackagePath, FileMode.Open))
        {
            // Load InstallComponents
            InstallEngineBase createPackageEngine = InstallEngineBase.GetInstallEngine(stream);
            // Package the file.
            result = createPackageEngine.RunInstall(this, installInfo.InstallPassKey);
        }

        // Send Final Result after we have Closed the File...client is responsible for deleting the file or not.
        SendInstallComplete(result);

        clientRequest = null;
    }

    internal void SendInstallProgress(InstallProgressInfo info)
    {
        if (lastInfo?.PercentComplete == info.PercentComplete &&
            (lastInfo?.Message == info.Message || info.UpdatePercentOnly))
            return; // Don't Send Duplication Notifications

        SendNotification(InstallMessageTypes.Ids.InstallProgress, info);

        lastInfo = info;
    }

    internal void SendLog(InstallLogInfo info)
    {
        SendNotification(InstallMessageTypes.Ids.InstallLog, info);
    }

    internal void SendInstallComplete(InstallCompleteInfo info)
    {
        SendResponse(clientRequest, info);

        // After Complete We Shut Down Runtime
        // Shutting Down From the Work Thread may Block
        Task.Run(() => 
        {
            AhsokaRuntime.Default.StopAllEndPoints();
        });
    }
}
