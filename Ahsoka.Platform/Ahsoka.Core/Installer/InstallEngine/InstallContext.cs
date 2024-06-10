using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.Installer.InstallEngine;

internal class InstallContext
{
    double percentComplete = 0;
    readonly double percentStep = 0;

    StreamWriter output;

    public LocalProgress<InstallProgressInfo> Progress { get; internal set; }
    public LocalProgress<InstallLogInfo> Log { get; internal set; }

    public InstallEngineBase InstallEngine { get; internal set; }
    public string PassKey { get; internal set; }
    private InstallService Service { get; set; }

    public InstallContext(InstallEngineBase installEngine, InstallService service, string passKey)
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

    public void CloseLog()
    {
        if (output != null)
            using (output)
            {
                output.Flush();
                output.Close();
            };
    }

    public void OpenLog()
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
