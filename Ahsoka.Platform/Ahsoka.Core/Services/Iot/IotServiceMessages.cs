using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Iot;

internal class IotServiceMessages : AhsokaMessagesBase
{
    public IotServiceMessages()
    {
        this.RegisterServiceRequest(IotMessageTypes.Ids.CreateClient, typeof(ClientInfo), typeof(ClientConnectionInfo));
        this.RegisterServiceRequest(IotMessageTypes.Ids.DestroyClient, typeof(ClientMessage), typeof(ClientConnectionInfo));

        this.RegisterServiceRequest(IotMessageTypes.Ids.SubscribeTopic, typeof(TopicSubscription), typeof(TopicResult));
        this.RegisterServiceRequest(IotMessageTypes.Ids.UnsubscribeTopic, typeof(TopicSubscription), typeof(TopicResult));
        this.RegisterServiceRequest(IotMessageTypes.Ids.PublishTopic, typeof(TopicMessage), typeof(TopicResult));

        this.RegisterServiceNotification(IotMessageTypes.Ids.TopicReceivedNotification, typeof(TopicMessage));
    }

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\IotService.proto", Properties.Resources.IotService);
        return result;
    }
}

