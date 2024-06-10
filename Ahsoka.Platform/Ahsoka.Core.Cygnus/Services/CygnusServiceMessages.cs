using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Cygnus;

internal class CygnusServiceMessage : AhsokaMessagesBase
{
    public CygnusServiceMessage()
    {
        RegisterServiceRequest(CygnusMessageTypes.Ids.EnableTransport,  typeof(CygnusServiceEnableDisableTransports), typeof(EmptyNotification));
        RegisterServiceRequest(CygnusMessageTypes.Ids.AddFile,          typeof(CygnusServiceFileInfo), typeof(EmptyNotification));
        RegisterServiceRequest(CygnusMessageTypes.Ids.AddCommand,       typeof(CygnusServiceCommandInfo), typeof(CygnusServiceCommandInfoResponse));
        RegisterServiceRequest(CygnusMessageTypes.Ids.EnableCli,        typeof(EmptyNotification), typeof(CygnusServiceEnableCLIResponse));
    }

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\CygnusService.proto", Properties.CygnusResources.CygnusService);
        result.Add("Ahsoka.Proto\\reach.proto", Properties.CygnusResources.reach);
        return result;
    }
}
