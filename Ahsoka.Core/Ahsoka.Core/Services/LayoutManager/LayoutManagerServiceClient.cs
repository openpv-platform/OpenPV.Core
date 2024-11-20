using Ahsoka.LayerManager;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.LayoutManager;
using Ahsoka.System;
using System;

namespace Ahsoka.Services.Iot;

/// <summary>
/// LayoutManager Client Working with Layout Service
/// </summary>
public class LayoutManagerServiceClient : AhsokaClientBase<LayoutManagerMessageTypes.Ids>
{
    /// <summary>
    /// Name for Service
    /// </summary>
    public static readonly string Name = "LayoutManagerService";

    /// <summary>
    /// Default Constructor that uses the System Default Configuration
    /// </summary>
    public LayoutManagerServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// </summary>
    /// <param name="config"></param>
    public LayoutManagerServiceClient(ServiceConfiguration config) :
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
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.StopLogo);
    }

    /// <summary>
    /// Update All Surface Properties with Single Call
    /// </summary>
    public void UpdateSurfaceSetup( SurfaceSetup setup)
    {
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.UpdateSurfaceSetup, setup);
    }

    /// <summary>
    /// Update Surface Position (Location to Draw on Screen)
    /// </summary>
    public void UpdateSurfaceLocation(SurfaceRectangle locationInfo)
    {
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.UpdateSurfaceLocation, locationInfo);
    }

    /// <summary>
    /// Update Surface Source (Location to Read From Buffer)
    /// </summary>
    public void UpdateSurfaceSource(SurfaceRectangle sourceInfo)
    {
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.UpdateSurfaceSource, sourceInfo);
    }

    /// <summary>
    /// Update Surface Source (Location to Read From Buffer)
    /// </summary>
    public void UpdateSurfaceVisibility(SurfaceVisibility visibility)
    {
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.UpdateSurfaceVisibility, visibility);
    }


    /// <summary>
    /// Update Draw Order of Surface ID's on Base Layer
    /// </summary>
    public void UpdateSurfaceZOrder(SurfaceOrder order)
    {
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.UpdateSurfaceZOrder, order);
    }

}
