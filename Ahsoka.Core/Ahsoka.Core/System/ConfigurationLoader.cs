using Ahsoka.ServiceFramework;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Linq;

namespace Ahsoka.System;

/// <summary>
/// Loads Ahsoka Endpoint Configurations
/// </summary>
public class ConfigurationLoader
{
    private static readonly object syncRoot = new();
    private static RuntimeConfiguration configuration;

    /// <summary>
    /// Gets a copy of the default configuration (Defaults to TCP Configuration)
    /// </summary>
    /// <returns></returns>
    public static RuntimeConfiguration CreateDefaultConfiguration(bool includeExtensions = true)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Reading Default TCP Configuration");
        var config = JsonUtility.Deserialize<RuntimeConfiguration>(Properties.Resources.OPVServicesConfiguration);
        if (includeExtensions)
        {
            Extensions.LoadExtensions();
            foreach (var item in Extensions.Configurations)
                config.Services.AddRange(item.ServiceConfigurations);
        }
        return config;
    }

    /// <summary>
    /// Specifies which default configuration to use when no configuration is specified.
    /// </summary>
    private static void LoadRuntimeConfig()
    {
        configuration = GetRuntimeConfiguration();

        if (configuration == null || configuration.Services.Count == 0)
            configuration = CreateDefaultConfiguration();
    } 

    /// <summary>
    /// Loads a custom runtime configuration as the Runtime Default Configuration.
    /// </summary>
    /// <param name="config"></param>
    public static void SetRuntimeConfiguration(RuntimeConfiguration config)
    {
        lock (syncRoot)
            configuration = config;
    }


    /// <summary>
    /// Gets a specific service / client endpoint configuration for the given service name
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ServiceConfiguration GetServiceConfig(string serviceName)
    {
        // If no config selected or set, pick one.
        lock (syncRoot)
        {
            if (configuration == null)
                LoadRuntimeConfig();
        }

        var result = configuration.Services.FirstOrDefault(x => x.ServiceName == serviceName);
        if (result == null)
        {
            var defaultConfig = CreateDefaultConfiguration();
            result = defaultConfig.Services.FirstOrDefault(x => x.ServiceName == serviceName);
            if (result != null)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Configuration not Found, Falling back to Default Configuration for Service {serviceName}");
            }
            else
            {
                result = new()
                {
                    ServiceName = serviceName,
                    DataChannel = Math.Min(7000, configuration.Services.Max(x => x.DataChannel) + 1),
                    Behaviors = EndPointBehaviors.AutoStart,
                    IsExtension = true,
                    SocketType = TransportSocketTypes.TcpSocket,
                    TcpListenAddress = "*",
                    TcpConnectionAddress = "localhost",
                };
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Configuration not Found, AutoGenerating Local Only Configuration for Service {serviceName} at DataChannel{result.DataChannel}.");
            }

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"To prevent his message, add the service to your service configuration.");
            configuration.Services.Add(result);
        }

        return new()
        {
            ServiceName = result.ServiceName,
            DataChannel = result.DataChannel,
            Behaviors = result.Behaviors,
            IsExtension = result.IsExtension,
            SocketType = result.SocketType,
            TcpListenAddress = result.TcpListenAddress,
            TcpConnectionAddress = result.TcpConnectionAddress,
        };
    }

    internal static RuntimeConfiguration GetRuntimeConfiguration()
    {
        try
        {
            string path = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.StartConfig);
            if (File.Exists(path))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Reading Local Protobuffer Configuration File");
                var file = new FileStream(path, FileMode.Open, FileAccess.Read);
                return ProtoBuf.Serializer.Deserialize<RuntimeConfiguration>(file);
            }
        }
        catch { } // Fallback to Default Configuration 

        return null;
    }

}

/// <summary>
/// Configuration for an Ahsoka Runtime Containing All Service Configurations.
/// </summary>
public partial class RuntimeConfiguration : ICanDeserializeBase
{
    internal override void SetProperty(string propertyName, string value)
    {
        base.SetProperty(propertyName, value);
    }

    internal override ICanDeserializeBase CreatePropertyObject(string propertyName)
    {
        var config = new ServiceConfiguration();
        this.Services.Add(config);
        return config;
    }

    /// <summary>
    /// Sets Defaults for Runtime Configurations
    /// </summary>
    public RuntimeConfiguration()
    {
        this.StartupDelay = 3;
    }
}


/// <summary>
/// Configuration Information for a Single Ahsoka Service
/// </summary>
public partial class ServiceConfiguration : ICanDeserializeBase
{
    internal override void ParseComplete()
    {
        base.ParseComplete();
        if (Behaviors == EndPointBehaviors.NoBehavior)
            Behaviors = EndPointBehaviors.AutoStart;
    }

    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "serviceName")
            this.ServiceName = value;
        else if (propertyName == "dataChannel")
            this.DataChannel = int.Parse(value);
        else if (propertyName == "tcpListenAddress")
            this.TcpListenAddress = value;
        else if (propertyName == "tcpConnectionAddress")
            this.TcpConnectionAddress = value;
        else if (propertyName == "socketType")
            this.SocketType = Enum.Parse<TransportSocketTypes>(value, true);
        else if (propertyName == "isExtension")
            this.IsExtension = bool.Parse(value);
        else if (propertyName == "behaviors")
            this.Behaviors = Enum.Parse<EndPointBehaviors>(value, true);
    }

    /// <summary>
    /// Produces the TCP URI for the data communication channel that is typically used by sockets utilizing ZMQ as the
    /// backing transport.
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public string DataURI
    {
        get
        {
            return $"tcp://{this.TcpConnectionAddress}:{this.DataChannel}";
        }
    }

    /// <summary>
    /// Produces the TCP URI for the data listening channel that is typically used by sockets utilizing ZMQ as the
    /// backing transport.
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public string DataListenURI
    {
        get
        {
            return $"tcp://{this.TcpListenAddress}:{this.DataChannel}";
        }
    }

}