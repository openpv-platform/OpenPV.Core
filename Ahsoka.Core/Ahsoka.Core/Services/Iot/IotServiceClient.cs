using Ahsoka.Core;

namespace Ahsoka.Services.Iot;

/// <summary>
/// IotService Client supporting MQTT for IoT Systems.
/// </summary>
public class IotServiceClient : AhsokaClientBase<IotMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor that uses the System Default Configuration
    /// </summary>
    public IotServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(IotService.Name))
    {

    }

    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// </summary>
    /// <param name="config"></param>
    public IotServiceClient(ServiceConfiguration config) :
        base(config, new IotServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new IotService(this.ServiceConfig);
    }


    /// <summary>
    /// Create a MQTT / IOT Client using the specified Broker url and login info
    /// </summary>
    /// <param name="clientInfo">Connection Information for MQTT Broker</param>
    /// <returns></returns>
    public ClientConnectionInfo CreateClient(ClientInfo clientInfo)
    {
        return SendMessageWithResponse<ClientConnectionInfo>(IotMessageTypes.Ids.CreateClient, clientInfo);
    }

    /// <summary>
    /// Destroy a previously created Client
    /// </summary>
    /// <param name="clientInfo">Connection Info for the MQTT Broker</param>
    /// <returns></returns>
    public ClientConnectionInfo DestroyClient(ClientMessage clientInfo)
    {
        return SendMessageWithResponse<ClientConnectionInfo>(IotMessageTypes.Ids.DestroyClient, clientInfo);
    }

    /// <summary>
    /// Subsribe to an MQTT Topic
    /// </summary>
    /// <param name="subscription">Topic to Subcribe</param>
    public TopicResult SubscribeTopic(TopicSubscription subscription)
    {
        return SendMessageWithResponse<TopicResult>(IotMessageTypes.Ids.SubscribeTopic, subscription);
    }

    /// <summary>
    ///  Unsubscribe to an MQTT Topic
    /// </summary>
    /// <param name="subscription">Topic to Unsubscribe</param>
    public TopicResult UnSubscribeTopic(TopicSubscription subscription)
    {
        return SendMessageWithResponse<TopicResult>(IotMessageTypes.Ids.UnsubscribeTopic, subscription);
    }

    /// <summary>
    /// Send a message to Subscribers of the Specified Topic
    /// </summary>
    /// <param name="message"></param>
    public TopicResult PublishTopic(TopicMessage message)
    {
        return SendMessageWithResponse<TopicResult>(IotMessageTypes.Ids.PublishTopic, message);
    }
}

