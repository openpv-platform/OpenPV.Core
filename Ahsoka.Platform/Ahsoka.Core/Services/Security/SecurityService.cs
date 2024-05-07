
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using BitMiracle.LibTiff.Classic;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Ahsoka.Services.Security;

/// <summary>
/// SecurityService
/// </summary>
[AhsokaService(SecurityService.Name)]
public class SecurityService : AhsokaServiceBase<SecurityMessageTypes.Ids>
{
    #region Fields
    /// <summary>
    /// Name asssociated with teh Security Service Configuration
    /// </summary>
    public const string Name = "SecurityService";
    SecurityConfiguration configuration;
    #endregion

    #region Methods

    /// <summary>
    /// Security Service Main Constructor
    /// </summary>
    public SecurityService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
    }

    /// <summary>
    /// Constructor for use when passing custom configurations.
    /// </summary>
    /// <param name="config"></param>
    public SecurityService(ServiceConfiguration config) :
        base(config, new SecurityServiceMessages())
    {
        var deviceSecurityFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.System.Hardware.InstallerPaths.ServiceSecurityFile);
        if (File.Exists(deviceSecurityFile))
            configuration = JsonUtility.Deserialize<SecurityConfiguration>(File.ReadAllText(deviceSecurityFile));

        TokenSource.RegisterTokenSource(new AppKeyTokenSource(configuration));
    }

    /// <summary>
    /// Handles all Service Messages
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<SecurityMessageTypes.Ids>())
        {
            case SecurityMessageTypes.Ids.RequestToken:

                HandleCreateToken(messageHeader, message as TokenRequestInfo);
                break;

            case SecurityMessageTypes.Ids.RequestChallengeToken:

                //HandleDestroyClient(messageHeader, message as ClientMessage);
                break;

            case SecurityMessageTypes.Ids.CompleteChallenge:

                //HandleTopic(messageHeader, message as TopicSubscription, true);
                break;

        }
    }

    private void HandleCreateToken(AhsokaMessageHeader messageHeader, TokenRequestInfo tokenRequestInfo)
    {
        var result = TokenSource.Default.UnlockEndPoint(tokenRequestInfo.ClientId, tokenRequestInfo.ApiKey, configuration.ApplicationId);
        SendMessageInternal(messageHeader, result);
    }
    /*

private void HandlePublishTopic(AhsokaMessageHeader messageHeader, TopicMessage topicMessage)
{
   TopicResult result = null;
   var client = GetClient(topicMessage.ClientId);
   try
   {
       client.NativeClient.PublishBinaryAsync(topicMessage.TopicName, topicMessage.Payload);
       result = new TopicResult() { ClientId = topicMessage.ClientId, Status = TopicResultStatus.Success };
   }
   catch (Exception ex)
   {
       result = new TopicResult() { ClientId = topicMessage.ClientId, Status = TopicResultStatus.Error, ResultText = ex.Message };
   }

   SendMessageInternal(messageHeader, result);
}

private void HandleTopic(AhsokaMessageHeader messageHeader, TopicSubscription topicSubscription, bool subscribe)
{
   TopicResult result = null;
   var client = GetClient(topicSubscription.ClientId);
   try
   {
       if (client.State == ClientState.Connected)
       {
           if (subscribe)
               client.NativeClient.SubscribeAsync(topicSubscription.TopicName, (MqttQualityOfServiceLevel)(int)topicSubscription.QualityOfService);
           else
               client.NativeClient.UnsubscribeAsync(topicSubscription.TopicName);

           result = new TopicResult() { ClientId = topicSubscription.ClientId, Status = TopicResultStatus.Success, ResultText = "Success" };
       }
       else
           result = new TopicResult() { ClientId = topicSubscription.ClientId, Status = TopicResultStatus.Error, ResultText = "Not Connected" };

   }
   catch (Exception ex)
   {
       result = new TopicResult() { ClientId = topicSubscription.ClientId, Status = TopicResultStatus.Error, ResultText = ex.Message };
   }

   SendMessageInternal(messageHeader, result);
}
*/
    #endregion

}


