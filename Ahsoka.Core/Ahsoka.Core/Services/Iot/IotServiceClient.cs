using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System.Text;

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
        return SendNotificationWithResponse<ClientConnectionInfo>(IotMessageTypes.Ids.CreateClient, clientInfo);
    }

    /// <summary>
    /// Destroy a previously created Client
    /// </summary>
    /// <param name="clientInfo">Connection Info for the MQTT Broker</param>
    /// <returns></returns>
    public ClientConnectionInfo DestroyClient(ClientMessage clientInfo)
    {
        return SendNotificationWithResponse<ClientConnectionInfo>(IotMessageTypes.Ids.DestroyClient, clientInfo);
    }

    /// <summary>
    /// Subsribe to an MQTT Topic
    /// </summary>
    /// <param name="subscription">Topic to Subcribe</param>
    public TopicResult SubscribeTopic(TopicSubscription subscription)
    {
        return SendNotificationWithResponse<TopicResult>(IotMessageTypes.Ids.SubscribeTopic, subscription);
    }

    /// <summary>
    ///  Unsubscribe to an MQTT Topic
    /// </summary>
    /// <param name="subscription">Topic to Unsubscribe</param>
    public TopicResult UnSubscribeTopic(TopicSubscription subscription)
    {
        return SendNotificationWithResponse<TopicResult>(IotMessageTypes.Ids.UnsubscribeTopic, subscription);
    }

    /// <summary>
    /// Send a message to Subscribers of the Specified Topic
    /// </summary>
    /// <param name="message"></param>
    public TopicResult PublishTopic(TopicMessage message)
    {
        return SendNotificationWithResponse<TopicResult>(IotMessageTypes.Ids.PublishTopic, message);
    }
}


/// <summary>
/// Topic Message for Sending and Receiving MQTT Messages
/// </summary>
public partial class TopicMessage
{
    /// <summary>
    /// Payload as a String
    /// </summary>
    public string PayloadString
    {
        get { return UTF8Encoding.UTF8.GetString(Payload); }
        set { Payload = UTF32Encoding.UTF8.GetBytes(value); }
    }
}