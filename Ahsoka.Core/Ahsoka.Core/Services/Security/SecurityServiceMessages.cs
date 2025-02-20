using Ahsoka.Core;

namespace Ahsoka.Services.Security;

internal class SecurityServiceMessages : AhsokaMessagesBase
{
    public SecurityServiceMessages() : base(SecurityService.Name)
    {
        // Client has a Public Key and can Sign it.
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.RequestToken, typeof(TokenRequestInfo), typeof(TokenRequestResponse));
        this.RegisterServiceNotification(SecurityMessageTypes.Ids.ChallengeReceived, typeof(ChallengeRequestInfo));
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.CompleteChallenge, typeof(ChallengeCompleteInfo), typeof(EmptyNotification));
        this.RegisterServiceNotification(SecurityMessageTypes.Ids.AuthorizationComplete, typeof(Token));
    }


    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaSecurity", typeof(SecurityMessageTypes.Ids));
    }

}