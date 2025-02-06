using Ahsoka.Core;

namespace Ahsoka.Services.Gateway;

internal class GatewayServiceMessages : AhsokaMessagesBase
{
    public GatewayServiceMessages() : base(GatewayService.Name)
    {
        //this.RegisterServiceRequest(SparkMessageTypes.Ids.SetSparkSubscription, typeof(SparkSubscription), typeof(EmptyNotification));
        //this.RegisterServiceNotification(SparkMessageTypes.Ids.AppChangeNotification, typeof(AppChangeInfo));
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaGateway", typeof(GatewayMessageTypes.Ids));
    }
}

