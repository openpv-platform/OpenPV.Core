using Ahsoka.Core;
using Ahsoka.Installer;
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

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\IotService.proto", Properties.Resources.IotService);
        return result;
    }
}

