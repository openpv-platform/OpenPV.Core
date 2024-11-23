using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System;

namespace Ahsoka.Services.LayerManager;

/// <summary>
/// LayoutManager Client Working with Layout Service
/// </summary>
public class LayerManagerServiceClient : AhsokaClientBase<LayerManagerMessageTypes.Ids>
{
    /// <summary>
    /// Name for Service
    /// </summary>
    public static readonly string Name = "LayoutManagerService";

    /// <summary>
    /// Default Constructor that uses the System Default Configuration
    /// </summary>
    public LayerManagerServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// </summary>
    /// <param name="config"></param>
    public LayerManagerServiceClient(ServiceConfiguration config) :
        base(config, new LayoutManagerMessages())
    {
        // Disable Auto Start
        config.Behaviors &= ~EndPointBehaviors.AutoStart;
    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return null;
    }

    /// <summary>
    /// Stop the Logo from showing in LayerManager
    /// </summary>
    public void StopLogo()
    {
        SendMessageWithResponse(LayerManagerMessageTypes.Ids.StopLogo);
    }

    /// <summary>
    /// Update All Surface Properties with Single Call
    /// </summary>
    public void UpdateSurfaceSetup( SurfaceSetup setup)
    {
        SendMessageWithResponse(LayerManagerMessageTypes.Ids.UpdateSurfaceSetup, setup);
    }

    /// <summary>
    /// Update All Surface Properties with Single Call
    /// </summary>
    public SurfaceSetup GetSurfaceSetup(SurfaceRequest request)
    {
        return SendMessageWithResponse<SurfaceSetup>(LayerManagerMessageTypes.Ids.GetSurfaceSetup, request);
    }

    /// <summary>
    /// Update Surface Position (Location to Draw on Screen)
    /// </summary>
    public void UpdateSurfaceLocation(SurfaceRectangle locationInfo)
    {
        SendMessageWithResponse(LayerManagerMessageTypes.Ids.UpdateSurfaceLocation, locationInfo);
    }

    /// <summary>
    /// Update Surface Source (Location to Read From Buffer)
    /// </summary>
    public void UpdateSurfaceSource(SurfaceRectangle sourceInfo)
    {
        SendMessageWithResponse(LayerManagerMessageTypes.Ids.UpdateSurfaceSource, sourceInfo);
    }

    /// <summary>
    /// Update Surface Source (Location to Read From Buffer)
    /// </summary>
    public void UpdateSurfaceVisibility(SurfaceVisibility visibility)
    {
        SendMessageWithResponse(LayerManagerMessageTypes.Ids.UpdateSurfaceVisibility, visibility);
    }


    /// <summary>
    /// Update Draw Order of Surface ID's on Base Layer
    /// </summary>
    public void UpdateSurfaceZOrder(SurfaceOrder order)
    {
        SendMessageWithResponse(LayerManagerMessageTypes.Ids.UpdateSurfaceZOrder, order);
    }

}
