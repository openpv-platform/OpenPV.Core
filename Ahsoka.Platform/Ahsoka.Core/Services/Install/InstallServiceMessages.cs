using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Install;

internal class InstallServiceMessages : AhsokaMessagesBase
{
    public InstallServiceMessages()
    {
        this.RegisterServiceRequest(InstallMessageTypes.Ids.InstallPackage, typeof(InstallPackageRequest), typeof(InstallCompleteInfo));
        this.RegisterServiceRequest(InstallMessageTypes.Ids.InstallProgress, typeof(InstallProgressInfo), typeof(InstallProgressInfo));
        this.RegisterServiceRequest(InstallMessageTypes.Ids.InstallLog, typeof(InstallLogInfo), typeof(InstallLogInfo));
    }
}
