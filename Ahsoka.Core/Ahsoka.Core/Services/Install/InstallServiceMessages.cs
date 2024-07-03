using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Install;

internal class InstallServiceMessages : AhsokaMessagesBase
{
    public InstallServiceMessages()
    {
        this.RegisterServiceRequest(InstallMessageTypes.Ids.InstallPackage, typeof(InstallPackageRequest), typeof(InstallCompleteInfo));
        this.RegisterServiceNotification(InstallMessageTypes.Ids.InstallProgress, typeof(InstallProgressInfo));
        this.RegisterServiceNotification(InstallMessageTypes.Ids.InstallLog, typeof(InstallLogInfo));
    }

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\InstallService.proto", Properties.Resources.InstallService);
        return result;
    }
}
