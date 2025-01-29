using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Security;

internal class SecurityServiceMessages : AhsokaMessagesBase
{
    public SecurityServiceMessages()
    {
        // Client has a Public Key and can Sign it.
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.RequestToken, typeof(TokenRequestInfo), typeof(TokenRequestResponse));
        this.RegisterServiceNotification(SecurityMessageTypes.Ids.ChallengeReceived, typeof(ChallengeRequestInfo));
        this.RegisterServiceRequest(SecurityMessageTypes.Ids.CompleteChallenge, typeof(ChallengeCompleteInfo), typeof(EmptyNotification));
        this.RegisterServiceNotification(SecurityMessageTypes.Ids.AuthorizationComplete, typeof(Token));
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\SecurityService.proto", Properties.Resources.SecurityService);
        return result;
    }
}