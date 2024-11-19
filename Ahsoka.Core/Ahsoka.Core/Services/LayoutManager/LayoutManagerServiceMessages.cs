using Ahsoka.Installer;
using Ahsoka.LayerManager;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.LayoutManager;

internal class LayoutManagerMessages : AhsokaMessagesBase
{
    public LayoutManagerMessages()
    {
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.CloseLogo, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.UpdateSurfaceLayout, typeof(SurfaceLayout), typeof(EmptyNotification));
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\LayoutManagerService.proto", Properties.Resources.IotService);
        return result;
    }
}

