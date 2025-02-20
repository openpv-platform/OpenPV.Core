using Ahsoka.Core;

namespace Ahsoka.Services.Iot;

internal class IotServiceMessages : AhsokaMessagesBase
{
    public IotServiceMessages() : base(IotService.Name)
    {
        this.RegisterServiceRequest(IotMessageTypes.Ids.CreateClient, typeof(ClientInfo), typeof(ClientConnectionInfo));
        this.RegisterServiceRequest(IotMessageTypes.Ids.DestroyClient, typeof(ClientMessage), typeof(ClientConnectionInfo));

        this.RegisterServiceRequest(IotMessageTypes.Ids.SubscribeTopic, typeof(TopicSubscription), typeof(TopicResult));
        this.RegisterServiceRequest(IotMessageTypes.Ids.UnsubscribeTopic, typeof(TopicSubscription), typeof(TopicResult));
        this.RegisterServiceRequest(IotMessageTypes.Ids.PublishTopic, typeof(TopicMessage), typeof(TopicResult));

        this.RegisterServiceNotification(IotMessageTypes.Ids.TopicReceivedNotification, typeof(TopicMessage));
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaIot", typeof(IotMessageTypes.Ids));
    }
}

