using System;
using System.Collections.Generic;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public abstract class AhsokaMessagesBase
{
    private readonly Dictionary<Enum, ServiceMessageInfo> info = new();
    internal Dictionary<Enum, ServiceMessageInfo> Info => info;

    protected void RegisterServiceRequest(Enum transportId, Type requestType, Type replyType, bool generateImplementation = true, bool hideInGeneratedClients = false)
    {
        Info.Add(transportId, new ServiceMessageInfo() { AutoGenerateImplementation = generateImplementation, HideInGeneratedClients = hideInGeneratedClients, TransportId = transportId, RequestType = requestType, ReplyType = replyType });
    }

    protected void RegisterServiceNotification(Enum transportId, Type notificationType, bool generateImplementation = true, bool hideInGeneratedClients = false)
    {
        Info.Add(transportId, new ServiceMessageInfo() { AutoGenerateImplementation = generateImplementation, HideInGeneratedClients = hideInGeneratedClients, IsNotification = true, TransportId = transportId, RequestType = notificationType, ReplyType = notificationType });
    }

    public ServiceMessageInfo GetMessageInfo(Enum transportType)
    {
        Info.TryGetValue(transportType, out ServiceMessageInfo returnValue);
        return returnValue;
    }
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
}
#endregion


#pragma warning restore CS1591