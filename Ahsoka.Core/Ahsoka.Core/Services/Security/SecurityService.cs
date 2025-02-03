using Ahsoka.Core;
using Ahsoka.Core.Utility;
using System.IO;
using System.Text;

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
        var deviceSecurityFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.Core.Hardware.InstallerPaths.ServiceSecurityFile);
        if (File.Exists(deviceSecurityFile))
        {
            var data = AesEncryption.DecryptFile(deviceSecurityFile, SystemInfo.HardwareInfo.FactoryInfo.DeviceId);
            configuration = JsonUtility.Deserialize<SecurityConfiguration>(Encoding.UTF8.GetString(data.ToArray()));
        }

        TokenSource.RegisterTokenSource(new AppKeyTokenSource(configuration));
    }

    /// <summary>
    /// Handles all Service Messages
    /// </summary>
    /// <param name="request"></param>
    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case SecurityMessageTypes.Ids.RequestToken:

                HandleCreateToken(request, request.Message as TokenRequestInfo);
                break;

            case SecurityMessageTypes.Ids.CompleteChallenge:

                HandleCompleteChallenge(request, request.Message as ChallengeCompleteInfo);
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
        var key = SystemInfo.HardwareInfo.GetDeviceSigningKey(false);
        if (!string.IsNullOrEmpty(key))
        {
            try
            {
                return RSAEncryption.VerifyDataSignature(clientIdSigned, clientId, key);
            }
            catch
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Error Decoding Signature");
            }
        }

        return false;

    }

    private void HandleCreateToken(AhsokaServiceRequest request, TokenRequestInfo tokenRequestInfo)
    {
        var resultToken = TokenSource.Default.RequestTokens(tokenRequestInfo, configuration.ApplicationId, out byte[] requestInfo);
        SendResponse(request, new TokenRequestResponse { RequestId = requestInfo });

        // Return Token Or Issue Challenge.
        if (configuration.RequireChallenge == ChallengeOptions.NotRequired)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "HandleCreateToken - Not Required");
            SendNotification(SecurityMessageTypes.Ids.AuthorizationComplete, resultToken);
        }
        else
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "HandleCreateToken - Required");
            SendNotification(SecurityMessageTypes.Ids.ChallengeReceived, new ChallengeRequestInfo()
            {
                RequestId = requestInfo,
                ClientCode = tokenRequestInfo.ClientCode,
                ClientDescription = tokenRequestInfo.ClientDescription
            });
        }

    }

    private void HandleCompleteChallenge(AhsokaServiceRequest messageHeader, ChallengeCompleteInfo challengeCompleteInfo)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "HandleCompleteChallenge");
        Token token = new Token() { RequestId = challengeCompleteInfo.RequestId };
        if (challengeCompleteInfo.AcceptRequest)
        {
            bool result = ValidateLocalSecret(challengeCompleteInfo.ClientIdSigned, messageHeader.Header.ClientId);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Validated Client Secret for Local Security Client with result {result}");
            if (result)
                token = TokenSource.Default.EnableToken(challengeCompleteInfo.RequestId, challengeCompleteInfo.ClientCode);
            else
                token = new Token() { RequestId = challengeCompleteInfo.RequestId, TokenIsValid = false };
        }

        // Reply To Challenge Complete
        SendResponse(messageHeader, token);

        // Send Notification Regardless to Release any Waiting Client
        SendNotification(SecurityMessageTypes.Ids.AuthorizationComplete, token);
    }
    #endregion

}


