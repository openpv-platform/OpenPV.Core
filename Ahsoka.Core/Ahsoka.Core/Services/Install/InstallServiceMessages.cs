using Ahsoka.Core;
using Ahsoka.Installer;
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

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\InstallService.proto", Properties.Resources.InstallService);
        return result;
    }

}
