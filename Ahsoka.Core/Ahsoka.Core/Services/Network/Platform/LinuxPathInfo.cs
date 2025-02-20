using Ahsoka.Core;
using System;
using System.IO;

namespace Ahsoka.Services.Network;

internal static class LinuxPathInfo
{
    static readonly string systemPath;
    static readonly string runtimePath;

    static LinuxPathInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            systemPath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetServiceDataPath(Ahsoka.Core.Hardware.ServiceDataPaths.NetworkProfileFolder), "etc/systemd");
            runtimePath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetServiceDataPath(Ahsoka.Core.Hardware.ServiceDataPaths.NetworkProfileFolder), "run/systemd");
        }
        else
        {
            systemPath = "/etc/systemd";
            runtimePath = "/run/systemd";
        }
    }

    public static string SystemManagerPath
    {
        get { return systemPath; }
    }

    public static string SystemManagerRuntimePath
    {
        get { return runtimePath; }
    }

    public static string CreateSystemManagerPath(string runtimePath)
    {
        return Path.Combine(SystemManagerPath, runtimePath);
    }

    public static string CreateSystemManagerRuntimePath(string runtimePath)
    {
        return Path.Combine(SystemManagerRuntimePath, runtimePath);
    }

}