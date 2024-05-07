using Ahsoka.ServiceFramework;
using Ahsoka.Services.Iot;
using Ahsoka.System;
using System.Text;

namespace Ahsoka.Services.Security;

/// <summary>
/// IotService Client supporting MQTT for IoT Systems.
/// </summary>
public class SecurityServiceClient : AhsokaClientBase<SecurityMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor that uses the System Default Configuration
    /// </summary>
    public SecurityServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(SecurityService.Name))
    {

    }

    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// </summary>
    /// <param name="config"></param>
    public SecurityServiceClient(ServiceConfiguration config) :
        base(config, new SecurityServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new SecurityService(this.ServiceConfig);
    }

    /// <summary>
    /// Request an authorization token by sending data signed with the devices public key
    /// </summary>
    /// <param name="tokenRequest">Connection Information for MQTT Broker</param>
    /// <returns></returns>
    public Token RequestToken(TokenRequestInfo tokenRequest)
    {
        return SendNotificationWithResponse<Token>(SecurityMessageTypes.Ids.RequestToken, tokenRequest);
    }

    /// <summary>
    /// For clients that do not have the devices public key, request a Challenge Response occcur on the Target
    /// </summary>
    /// <param name="challengeRequest">Challenge Request Including Client Generated Code</param>
    /// <returns></returns>
    public EmptyNotification RequestChallenge(ChallengeRequestInfo challengeRequest)
    {
        return SendNotificationWithResponse<EmptyNotification>(SecurityMessageTypes.Ids.RequestChallengeToken, challengeRequest);
    }

    /// <summary>
    /// Accept the Challenge Value
    /// </summary>
    /// <param name="completeInfo">Challenge Completed</param>
    public TopicResult CompleteChallenge(ChallengeCompleteInfo completeInfo)
    {
        return SendNotificationWithResponse<TopicResult>(SecurityMessageTypes.Ids.CompleteChallenge, completeInfo);
    }

}
