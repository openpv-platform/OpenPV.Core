using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using System;
using System.IO;
using System.Linq;

namespace Ahsoka.Core;

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
        var config = JsonUtility.DeserializeWithTypeInfo<RuntimeConfiguration>(Properties.Resources.OPVServicesConfiguration);
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
