using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Services.Gateway;


/// <summary>
/// Spark Service Manages OTA Updates
/// </summary>
[AhsokaService(Name)]
public class GatewayService : GatewayServiceBase<GatewayMessageTypes.Ids>
{
    #region Fields
    GatewaySocket gatewaySocket = new();
    internal const string Name = "GatewayService";
    #endregion

    #region Methods

    /// <summary>
    /// Default CTOR
    /// </summary>
    public GatewayService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
    }

    /// <summary>
    /// CTOR Accepting Custom Configuration
    /// </summary>
    /// <param name="config"></param>
    public GatewayService(ServiceConfiguration config) :
        base(config, new GatewayServiceMessages())
    {

    }


    /// <summary>
    ///  Route Messages to Gateway Socket if EndPoint doesn't match
    /// </summary>
    /// <param name="serviceSocket"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    protected internal override bool OnInterseptMessage(IAhsokaServiceSocket serviceSocket, AhsokaServiceMessage message)
    {
        // If GatewayEnabled, route any EndPoint Messages to the Gateway
        GatewaySocket.RouteMessageToEndPoint(serviceSocket, message);
        return true;
    }


    #endregion
}
