using Ahsoka.Installer;
using Ahsoka.LayerManager;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.LayoutManager;

internal class LayoutManagerMessages : AhsokaMessagesBase
{
    public LayoutManagerMessages()
    {
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.StopLogo, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.UpdateSurfaceSetup, typeof(SurfaceSetup), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.UpdateSurfaceLocation, typeof(SurfaceRectangle), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.UpdateSurfaceSource, typeof(SurfaceRectangle), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.UpdateSurfaceVisibility, typeof(SurfaceVisibility), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayoutManagerMessageTypes.Ids.UpdateSurfaceZOrder, typeof(SurfaceOrder), typeof(EmptyNotification));
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\LayoutManagerService.proto", Properties.Resources.IotService);
        return result;
    }
}

