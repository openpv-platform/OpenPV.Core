using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IHasParameters
{
    SystemParameterData GetParameters(PackageInformation info);
}

public interface IHasEvents
{
    SystemEventData GetEvents(PackageInformation info);
}

public abstract class AhsokaMessagesBase: IHasParameters, IHasEvents
{
    #region Fields
    private readonly Dictionary<Int32, ServiceMessageInfo> info = new();
    internal Dictionary<Int32, ServiceMessageInfo> Info => info;
    #endregion

    #region Constructor
    public AhsokaMessagesBase()
    {
        // Add Invalid Types to All Services
        Info.Add(CoreTransportIds.InvalidToken.EnumToInt(), new ServiceMessageInfo() {TransportId = CoreTransportIds.InvalidToken, RequestType = typeof(EmptyNotification), ReplyType = typeof (EmptyNotification), IsNotification = true, HideInGeneratedClients = true });
        Info.Add(CoreTransportIds.RegisterClient.EnumToInt(), new ServiceMessageInfo() { TransportId = CoreTransportIds.RegisterClient, RequestType = typeof(ClientRegistration), ReplyType = typeof(ClientRegistration), IsNotification = true, HideInGeneratedClients = true });
        Info.Add(CoreTransportIds.DeregisterClient.EnumToInt(), new ServiceMessageInfo() { TransportId = CoreTransportIds.DeregisterClient, RequestType = typeof(ClientRegistration), ReplyType = typeof(ClientRegistration), IsNotification = true, HideInGeneratedClients = true });

        // Returned if the service throws an error
        Info.Add(CoreTransportIds.ServiceErrorThrown.EnumToInt(), new ServiceMessageInfo() { TransportId = CoreTransportIds.ServiceErrorThrown, RequestType = typeof(ServiceErrorThrownResponse), ReplyType = typeof(ServiceErrorThrownResponse), IsNotification = false, HideInGeneratedClients = true });
    }
    #endregion

    #region Service Registration Messages
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
    #endregion

    #region Client Generation Support
    public virtual Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        return OnGetAdditionalClientResources(type, includeImplementation);
    }

    public string GetInteropIncludes(string name, ApplicationType type)
    {
        return OnGetInteropIncludes(name, type);    
    }

    protected virtual Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        Dictionary<string, byte[]> newList = new()
        {
            { "Ahsoka.Proto\\Configuration.proto", Properties.Resources.Configuration },
            { "Ahsoka.Proto\\Functions.cmake", Properties.Resources.Functions },
            { "Ahsoka.Proto\\CMakeLists.txt", Properties.Resources.CMakeLists }
        };
        return newList;
    }

    protected virtual string OnGetInteropIncludes(string name, ApplicationType type)
    {
        return $"#include \"{name}.pb.h\"\n#include \"{name}Client.h\"\n"; ;
    }
    #endregion

    #region Parameter Values
    /// <summary>
    /// Fetch all known values in the Loaded Extensions and Core
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, SystemParameterData> GetAllSystemParameters(PackageInformation info = null)
    {
        Dictionary<string, SystemParameterData> allValues = new();

        foreach (var value in ClassLoader.GetItemsWithInterface(typeof(IHasParameters)))
        {
            IHasParameters service = ClassLoader.CreateType(value, null) as IHasParameters;
            var values = service.GetParameters(info);
            if (values != null && values.Count > 0)
                allValues[values.SystemName] = values;
        }

        return allValues;
    }

    /// <summary>
    /// Gets a list of Parameters Available to the System
    /// </summary>
    /// <returns></returns>
    SystemParameterData IHasParameters.GetParameters(PackageInformation info)
    {
        var values = new SystemParameterData();

        OnGetParameters(out string service, values, info);
        if(service != string.Empty)
            values.AssignKeys(service);

        return values;
    }

   

    /// <summary>
    /// Pupulate the Parameters Availabe in the System
    /// </summary>
    /// <param name="service"></param>
    /// <param name="values"></param>
    /// <param name="packageInfo"></param>
    protected virtual void OnGetParameters(out string service, List<ParameterData> values, PackageInformation packageInfo)
    {
        service = string.Empty;
    }
    #endregion

    #region Event Values
    /// <summary>
    /// Fetch all known event configs in the Loaded Extensions and Core
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, SystemEventData> GetAllServiceEvents(PackageInformation info = null)
    {
        Dictionary<string, SystemEventData> allValues = new();

        foreach (var value in ClassLoader.GetItemsWithInterface(typeof(IHasEvents)))
        {
            IHasEvents service = ClassLoader.CreateType(value, null) as IHasEvents;
            var config = service.GetEvents(info);
            if (config != null && config.Count > 0)
                allValues[service.ToString()] = config;
        }

        return allValues;
    }

    /// <summary>
    /// Gets the event config for a service
    /// </summary>
    /// <returns></returns>
    SystemEventData IHasEvents.GetEvents(PackageInformation info)
    {
        var values = new SystemEventData();

        OnGetEvents(out string service, values, info);
        if (service != string.Empty)
            values.AssignKeys(service);

        return values;
    }

    protected virtual void OnGetEvents(out string service, List<EventData> values, PackageInformation packageInfo)
    {
        service = string.Empty;
    }
    #endregion
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