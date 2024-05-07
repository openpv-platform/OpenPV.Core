using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Security;

internal class SecurityServiceMessages : AhsokaMessagesBase
{
    public SecurityServiceMessages()
    {
        // Client has a Public Key and can Sign it.
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.RequestToken, typeof(TokenRequestInfo), typeof(Token));

        // Client must request access from local device.
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.RequestChallengeToken, typeof(ChallengeRequestInfo), typeof(EmptyNotification));
        this.RegisterServiceNotification(SecurityMessageTypes.Ids.ChallengeReceived, typeof(ChallengeRequestInfo));
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.CompleteChallenge, typeof(ChallengeCompleteInfo), typeof(EmptyNotification));
        this.RegisterServiceNotification(SecurityMessageTypes.Ids.AuthorizationComplete, typeof(Token));
    }
}