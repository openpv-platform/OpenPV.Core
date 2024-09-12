using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.Installer.InstallEngine;

/// <summary>
///  Context Object for referencing Shared Progress and Logging During Install Creation.
/// </summary>
public class InstallContext
{
    double percentComplete = 0;
    readonly double percentStep = 0;

    StreamWriter output;

    /// <summary>
    ///  Shared Progress Object
    /// </summary>
    public LocalProgress<InstallProgressInfo> Progress { get; internal set; }

    /// <summary>
    /// Shared Logging 
    /// </summary>
    public LocalProgress<InstallLogInfo> Log { get; internal set; }

    /// <summary>
    ///  Shared Pointer to current Install Engine
    /// </summary>
    public InstallEngineBase InstallEngine { get; internal set; }

    internal string PassKey { get;  set; }
    private InstallService Service { get; set; }

    internal InstallContext(InstallEngineBase installEngine, InstallService service, string passKey)
    {
        Service = service;
        InstallEngine = installEngine;

        percentStep = 100.0 / InstallEngine.PackageContents.Components.Count;

        Progress = new(info =>
        {
            double currentPercent = percentComplete + (percentStep * ((double)info.PercentComplete / 100));
            info.PercentComplete = (int)currentPercent;
            service.SendInstallProgress(info);
        });

        Log = new(info =>
        {
            output?.WriteLine($"{DateTime.Now:O} - {info.LogMessageType}: {info.LogMessage}");
            Service.SendLog(info);
        });
    }

    internal void CloseLog()
    {
        if (output != null)
            using (output)
            {
                output.Flush();
                output.Close();
            };
    }

    internal void OpenLog()
    {
        string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        if (SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.LogRoot) != null)
        {
            string installLogPath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.LogRoot), "installer");
            if (!Directory.Exists(installLogPath))
                Directory.CreateDirectory(installLogPath);

            output = new StreamWriter(new FileStream(Path.Combine(installLogPath, $"install-{timeStamp}.log"), FileMode.Create))
            {
                AutoFlush = true // Ensure we write logs quickly
            };
        }

    }

    internal void MoveNext()
    {
        percentComplete += percentStep;
    }
}
