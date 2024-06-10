using Ahsoka.ServiceFramework;

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
}
