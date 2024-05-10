using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public abstract class AhsokaMessagesBase
{
    private readonly Dictionary<Int32, ServiceMessageInfo> info = new();
    internal Dictionary<Int32, ServiceMessageInfo> Info => info;

    public AhsokaMessagesBase()
    {
        // Add Invalid Types to All Services
        Info.Add(CoreTransportIds.InvalidToken.EnumToInt(), new ServiceMessageInfo() {TransportId = CoreTransportIds.InvalidToken, RequestType = typeof(InvalidResponseNotification), ReplyType = typeof (InvalidResponseNotification), IsNotification = true, HideInGeneratedClients = true });
        Info.Add(CoreTransportIds.RegisterClient.EnumToInt(), new ServiceMessageInfo() { TransportId = CoreTransportIds.RegisterClient, RequestType = typeof(ClientRegistration), ReplyType = typeof(ClientRegistration), IsNotification = true, HideInGeneratedClients = true });
        Info.Add(CoreTransportIds.DeregisterClient.EnumToInt(), new ServiceMessageInfo() { TransportId = CoreTransportIds.DeregisterClient, RequestType = typeof(ClientRegistration), ReplyType = typeof(ClientRegistration), IsNotification = true, HideInGeneratedClients = true });
    }

    protected void RegisterServiceRequest(Enum transportId, Type requestType, Type replyType, bool generateImplementation = true, bool hideInGeneratedClients = false, bool requireSecurity = true)
    {
        Info.Add(transportId.EnumToInt(), new ServiceMessageInfo() { AutoGenerateImplementation = generateImplementation, HideInGeneratedClients = hideInGeneratedClients, TransportId = transportId, RequestType = requestType, ReplyType = replyType, RequiresSecurity = requireSecurity });
    }

    protected void RegisterServiceNotification(Enum transportId, Type notificationType, bool generateImplementation = true, bool hideInGeneratedClients = false)
    {
        Info.Add(transportId.EnumToInt(), new ServiceMessageInfo() { AutoGenerateImplementation = generateImplementation, HideInGeneratedClients = hideInGeneratedClients, IsNotification = true, TransportId = transportId, RequestType = notificationType, ReplyType = notificationType });
    }

    public ServiceMessageInfo GetMessageInfo(Enum transportType)
    {
        Info.TryGetValue(transportType.EnumToInt(), out ServiceMessageInfo returnValue);
        return returnValue;
    }

    public virtual Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        return new();
    }

    public virtual string GetInteropIncludes(string name, ApplicationType type)
    {
        return $"#include \"{name}.pb.h\"\n#include \"{name}Client.h\"\n"; ;
    }

    public virtual KnownDataKeys GetKnownValues() { return null; }
}

#region ServiceRequestInfo
public class ServiceMessageInfo
{
    public bool HideInGeneratedClients = false;
    public bool AutoGenerateImplementation = true;
    public bool IsNotification = false;
    public Enum TransportId;
    public Type RequestType { get; internal set; }
    public Type ReplyType { get; internal set; }
    public bool RequiresSecurity { get; internal set; } = true;
}
#endregion


#pragma warning restore CS1591