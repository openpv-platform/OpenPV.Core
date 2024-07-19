
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System.IO;

namespace Ahsoka.Services.Security;

/// <summary>
/// SecurityService
/// </summary>
[AhsokaService(Name)]
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

            case SecurityMessageTypes.Ids.CompleteChallenge:

                HandleCompleteChallenge(messageHeader, message as ChallengeCompleteInfo);
                break;

        }
    }

    /// <summary>
    /// Validate that the Secret is correct and return result.
    /// </summary>
    /// <param name="clientIdSigned"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    protected bool ValidateLocalSecret(byte[] clientIdSigned, byte[] clientId)
    {
        if (clientIdSigned == null || clientIdSigned.Length == 0)
            return false;

        // We sign with the device Signature here to prove we are local.
        var path = SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyPEMFile);
        if (File.Exists(path))
        {
            try
            {
                return RSAEncryption.VerifyDataSignature(clientIdSigned, clientId, File.ReadAllText(path));
            }
            catch { }
        }

        return false;

    }

    private void HandleCreateToken(AhsokaMessageHeader messageHeader, TokenRequestInfo tokenRequestInfo)
    {
        var resultToken = TokenSource.Default.RequestTokens(tokenRequestInfo, configuration.ApplicationId, out byte[] request);
        SendMessageInternal(messageHeader, new TokenRequestResponse { RequestId = request });

        // Return Token Or Issue Challenge.
        if (configuration.RequireChallenge == ChallengeOptions.NotRequired)
        {
            messageHeader.TransportId = SecurityMessageTypes.Ids.AuthorizationComplete.EnumToInt();
            SendMessageInternal(messageHeader, resultToken, true);
        }
        else
        {
            messageHeader.TransportId = SecurityMessageTypes.Ids.ChallengeReceived.EnumToInt();
            SendMessageInternal(messageHeader, new ChallengeRequestInfo()
            {
                RequestId = request,
                ClientCode = tokenRequestInfo.ClientCode,
                ClientDescription = tokenRequestInfo.ClientDescription
            }, true);
        }

    }
    
    private void HandleCompleteChallenge(AhsokaMessageHeader messageHeader, ChallengeCompleteInfo challengeCompleteInfo)
    {
        Token token = new Token() { RequestId = challengeCompleteInfo.RequestId };
        if (challengeCompleteInfo.AcceptRequest)
        {
            bool result = ValidateLocalSecret(challengeCompleteInfo.ClientIdSigned, messageHeader.ClientId);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Validated Client Secret for Local Security Client with result {result}");
            if (result)
                token = TokenSource.Default.EnableToken(challengeCompleteInfo.RequestId, challengeCompleteInfo.ClientCode);
            else
                token = new Token() { RequestId=challengeCompleteInfo.RequestId, TokenIsValid = false };
        }

        // Reply To Challenge Complete
        SendMessageInternal(messageHeader, token);

        // Send Notification Regardless to Release any Waiting Client
        messageHeader.TransportId = SecurityMessageTypes.Ids.AuthorizationComplete.EnumToInt();
        SendMessageInternal(messageHeader, token);
    }
    #endregion

}


