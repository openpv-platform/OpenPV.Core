using Ahsoka.Core;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.LayerManager;

internal class LayoutManagerMessages : AhsokaMessagesBase
{
    public LayoutManagerMessages()
    {
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.StopLogo, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceSetup, typeof(SurfaceSetup), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.GetSurfaceSetup, typeof(SurfaceRequest), typeof(SurfaceSetup));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceLocation, typeof(SurfaceRectangle), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceSource, typeof(SurfaceRectangle), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceVisibility, typeof(SurfaceVisibility), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceZOrder, typeof(SurfaceOrder), typeof(EmptyNotification));
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\LayoutManagerService.proto", Properties.Resources.IotService);
        return result;
    }
}

