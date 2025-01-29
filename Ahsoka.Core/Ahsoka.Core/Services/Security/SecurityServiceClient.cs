using Ahsoka.ServiceFramework;
using Ahsoka.Services.Iot;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
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
        lock (requests) // Protect the List until we get response so notification doesn't get stepped on.
        {
            var response = SendMessageWithResponse<TokenRequestResponse>(SecurityMessageTypes.Ids.RequestToken, tokenRequest);
            if (usingWaitMethod)
                requests[response.RequestId.ToGuid()] = null;

            return response;
        }

    }

    /// <summary>
    /// Accept the Challenge Value
    /// </summary>
    /// <param name="completeInfo">Challenge Completed</param>
    public TopicResult CompleteChallenge(ChallengeCompleteInfo completeInfo)
    {
        return SendMessageWithResponse<TopicResult>(SecurityMessageTypes.Ids.CompleteChallenge, completeInfo);
    }

    /// <summary>
    /// Handle Simple Token Requests
    /// </summary>
    /// <param name="args"></param>
    protected override void OnReceiveNotification(AhsokaNotificationArgs args)
    {
        if (args.TransportId == SecurityMessageTypes.Ids.AuthorizationComplete && args.NotificationObject is Token token)
        {
            // Accept Challenge Token and Release WaitEvent
            lock (requests)
            {
                if (token.RequestId != null)
                    requests[token.RequestId.ToGuid()] = token;
            }
            waitForChallengeAccept.Set();

            args.ShouldRaiseEvent = false;
        }

    }

    /// <summary>
    /// Waits for a token matching the requestid - Requires usingWaitMethod = True when calling RequestToken
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    public Token WaitForToken(byte[] requestId)
    {
        Token token = null;
        var requestGuid = requestId.ToGuid();
        waitForChallengeAccept.WaitOne();

        lock (requests)
        {
            token = requests[requestGuid];
            requests.Remove(requestGuid);
        }


        return token;
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
                var data = AesEncryption.DecryptFile(path, SystemInfo.HardwareInfo.FactoryInfo.DeviceId);
                string key = Encoding.UTF8.GetString(data.ToArray());

                return RSAEncryption.GetSignatureWithKey(((IAhsokaClientEndPoint)this).GetClientId(), key);
            }
            catch { }
        }

        return null;
    }
}
