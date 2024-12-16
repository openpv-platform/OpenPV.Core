using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Gateway;

internal class GatewayServiceMessages : AhsokaMessagesBase
{
    public GatewayServiceMessages()
    {
        //this.RegisterServiceRequest(SparkMessageTypes.Ids.SetSparkSubscription, typeof(SparkSubscription), typeof(EmptyNotification));
        //this.RegisterServiceNotification(SparkMessageTypes.Ids.AppChangeNotification, typeof(AppChangeInfo));
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\GatewayService.proto", Properties.Resources.SparkService);
        return result;
    }
}

