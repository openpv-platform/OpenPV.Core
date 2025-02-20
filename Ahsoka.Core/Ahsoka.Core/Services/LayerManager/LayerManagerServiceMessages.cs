using Ahsoka.Core;

namespace Ahsoka.Services.LayerManager;

internal class LayoutManagerMessages : AhsokaMessagesBase
{
    public LayoutManagerMessages() : base(LayerManagerServiceClient.Name)
    {
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.StopLogo, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceSetup, typeof(SurfaceSetup), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.GetSurfaceSetup, typeof(SurfaceRequest), typeof(SurfaceSetup));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceLocation, typeof(SurfaceRectangle), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceSource, typeof(SurfaceRectangle), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceVisibility, typeof(SurfaceVisibility), typeof(EmptyNotification));
        this.RegisterServiceRequest(LayerManagerMessageTypes.Ids.UpdateSurfaceZOrder, typeof(SurfaceOrder), typeof(EmptyNotification));
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaLayerManager", typeof(LayerManagerMessageTypes.Ids), typeof(LayerManagerConfiguration));
    }
}

