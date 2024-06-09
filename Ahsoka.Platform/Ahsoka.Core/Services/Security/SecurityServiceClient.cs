using Ahsoka.ServiceFramework;
using Ahsoka.Services.Iot;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Ahsoka.Services.Security;

/// <summary>
/// Security Service for use in Authorizing Services for Clients
/// </summary>
public class SecurityServiceClient : AhsokaClientBase<SecurityMessageTypes.Ids>
{
    AutoResetEvent waitForChallengeAccept = new(false);
    Dictionary<Guid, Token> requests = new Dictionary<Guid, Token>();

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
    /// Request an authorization token by sending an API Key to the Security service to get a token for the specified Clients.
    /// </summary>
    /// <param name="tokenRequest">Token Request Info</param>
    /// <param name="usingWaitMethod">Add the Request to the Wait List for use with WaitForToken method</param>
    /// 
    /// <returns></returns>
    public TokenRequestResponse RequestToken(TokenRequestInfo tokenRequest, bool usingWaitMethod = true)
    {
        var response = SendNotificationWithResponse<TokenRequestResponse>(SecurityMessageTypes.Ids.RequestToken, tokenRequest);
        if (usingWaitMethod)
            lock (requests)
                requests.Add(response.RequestId.ToGuid(), null);
        return response;
    }

    /// <summary>
    /// Accept the Challenge Value
    /// </summary>
    /// <param name="completeInfo">Challenge Completed</param>
    public TopicResult CompleteChallenge(ChallengeCompleteInfo completeInfo)
    {
        return SendNotificationWithResponse<TopicResult>(SecurityMessageTypes.Ids.CompleteChallenge, completeInfo);
    }

    /// <summary>
    /// Handle Simple Token Requests
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        if (messageHeader.TransportId == SecurityMessageTypes.Ids.AuthorizationComplete.EnumToInt() && message is Token token)
        {
            // Accept Challenge Token and Release WaitEvent
            lock (requests)
                requests[token.RequestId.ToGuid()] = token;

            waitForChallengeAccept.Set();
        }

        base.OnHandleReceive(messageHeader, message);
    }

    /// <summary>
    /// Waits for a token matching the requestid - Requires usingWaitMethod = True when calling RequestToken
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    public Token WaitForToken(byte[] requestId)
    {
        var requestGuid = requestId.ToGuid();
        waitForChallengeAccept.WaitOne();

        lock (requests)
        {
            var token = requests[requestGuid];
            requests.Remove(requestGuid);
            return token;
        }
    }

    /// <summary>
    /// Create a Signature to be validated on the service side.
    /// </summary>
    /// <returns></returns>
    public byte[] GetLocalSecret()
    {
        // We sign with the device Signature here to prove we are local.
        var path = SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyPEMFile);
        if (File.Exists(path))
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Generating Client Secret for Local Security Client");
            try
            {
                return RSAEncryption.GetSignatureWithKey(((IAhsokaClientEndpoint)this).GetClientId(), File.ReadAllText(path));
            }
            catch {}
        }

        return null;
    }
}
