using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Install;

internal class InstallServiceMessages : AhsokaMessagesBase
{
    public InstallServiceMessages()
    {
        this.RegisterServiceRequest(InstallMessageTypes.Ids.InstallPackage, typeof(InstallPackageRequest), typeof(InstallCompleteInfo));
        this.RegisterServiceNotification(InstallMessageTypes.Ids.InstallProgress, typeof(InstallProgressInfo));
        this.RegisterServiceNotification(InstallMessageTypes.Ids.InstallLog, typeof(InstallLogInfo));
    }
}
