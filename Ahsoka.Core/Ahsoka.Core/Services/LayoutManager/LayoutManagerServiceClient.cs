using Ahsoka.LayerManager;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.LayoutManager;
using Ahsoka.System;

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
    public void CloseLogo()
    {
        SendMessageWithResponse(LayoutManagerMessageTypes.Ids.CloseLogo);
    }

    /// <summary>
    /// Update Surface Layout in LayerManager
    /// </summary>
    public void UpdateSurfaceLayout( SurfaceLayout layout)
    {
        var result = SendMessageWithResponse<SurfaceLayout>(LayoutManagerMessageTypes.Ids.UpdateSurfaceLayout, layout);
    }
}
