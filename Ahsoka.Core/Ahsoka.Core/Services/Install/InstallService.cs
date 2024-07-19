#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.Install;

[AhsokaService(Name)]
public class InstallService : AhsokaServiceBase<InstallMessageTypes.Ids>
{
    public const string Name = "InstallService";
    InstallProgressInfo lastInfo;
    AhsokaMessageHeader clientHeader = null;

    public InstallService()
        : this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public InstallService(ServiceConfiguration config) :
        base(config, new InstallServiceMessages())
    {

    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<InstallMessageTypes.Ids>())
        {
            case InstallMessageTypes.Ids.InstallPackage:

                InstallPackageRequest request = message as InstallPackageRequest;
                ExecuteInstall(messageHeader, request);
                break;
        }
    }

    private void ExecuteInstall(AhsokaMessageHeader messageHeader, InstallPackageRequest request)
    {
        // If the client header is not null another install must be in progress.
        if (clientHeader != null)
        {
            SendInstallComplete(new InstallCompleteInfo() { Message = "Install in Progress", Result = InstallResult.PreviousInstallInProgress });
            return;
        }

        // Capture the client header so we can send responses back to the correct client
        this.clientHeader = messageHeader;

        InstallCompleteInfo result = null;

        // Stream will be closed after RunInstall is completed
        using (var stream = new FileStream(request.InstallPackagePath, FileMode.Open))
        {
            // Load InstallComponents
            InstallEngineBase createPackageEngine = InstallEngineBase.GetInstallEngine(stream);
            // Package the file.
            result = createPackageEngine.RunInstall(this, request.InstallPassKey);
        }

        // Send Final Result after we have Closed the File...client is responsible for deleting the file or not.
        SendInstallComplete(result);

        clientHeader = null;
    }

    internal void SendInstallProgress(InstallProgressInfo info)
    {
        if (lastInfo?.PercentComplete == info.PercentComplete &&
            (lastInfo?.Message == info.Message || info.UpdatePercentOnly))
            return; // Don't Send Duplication Notifications

        var reply = CreateReplyMessageHeader(clientHeader, InstallMessageTypes.Ids.InstallProgress);

        SendMessageInternal(reply, info);

        lastInfo = info;
    }

    internal void SendLog(InstallLogInfo info)
    {
        var reply = CreateReplyMessageHeader(clientHeader, InstallMessageTypes.Ids.InstallLog);

        reply.TransportId = InstallMessageTypes.Ids.InstallLog.EnumToInt();
        SendMessageInternal(reply, info);
    }

    internal void SendInstallComplete(InstallCompleteInfo info)
    {
        SendMessageInternal(clientHeader, info);

        // After Complete We Shut Down Runtime
        // Shutting Down From the Work Thread may Block
        Task.Run(() => 
        {
            AhsokaRuntime.Default.DisconnectAllEndPoints();
        });
    }
}
