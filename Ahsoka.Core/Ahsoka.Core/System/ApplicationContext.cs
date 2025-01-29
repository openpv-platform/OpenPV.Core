using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using Microsoft.Extensions.Hosting.Systemd;
using Mono.Unix;
using Mono.Unix.Native;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.System;

/// <summary>
/// Application Context object that allows the Application to block
/// waiting on a linux or windows signals.
/// </summary>
public static class ApplicationContext
{
    static readonly ManualResetEventSlim reset = new();

    /// <summary>
    /// Releases startup to continue on Linux system
    /// </summary>
    public static void ReleaseStartup()
    {
        SystemdNotifier notifier = new();
        notifier.Notify(ServiceState.Ready);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Releasing Startup (System Notify)");
    }

    /// <summary>
    /// Starts a WebBrowser returning a task which can be use to wait for the browser to shut down
    /// </summary>
    /// <param name="url"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static Task StartEmbeddedBrowser(string url, CancellationToken token)
    {
        // Startup Browser in an Async Thread
        return Task.Run(() =>
        {
            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, info.Data);
            });

            if (!OperatingSystem.IsWindows())
                ProcessUtility.RunProcessAsync("web-browser", url, null, progressText, token);
        });
    }

    /// <summary>
    /// Block until a signal is received to shutdown from SystemD (Linux) or the Application is closed (Windows)
    /// </summary>
    public static void WaitForExitSignal()
    {
        if (OperatingSystem.IsWindows())
        {
            reset.Wait();
        }
        else
        {
            UnixSignal sigterm = new(Signum.SIGTERM);
            UnixSignal.WaitAny(new[] { sigterm });
        }
    }

    /// <summary>
    /// Ask the Application to Exit
    /// </summary>
    public static void Exit()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Stopping Default Dispatcher");
        Dispatcher.Default.Stop();

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Shutting Down Runtimes");
        AhsokaRuntime.Default.StopAllEndPoints();

        if (OperatingSystem.IsWindows())
        {
            reset.Set();
        }
        else
        {
            // Execute a System Shutdown.  We use --no-block so we don't stop here and cleanly exit.
            // we must send the stop since other processes may be running in the same service and we 
            // want them to shutdown as well.
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Executing Ahsoka.Application Shutdown (Stopping Service)");
            ProcessUtility.RunProcess("systemctl", "--no-block stop Ahsoka.Application", null, out string result, out string error);

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Executing Ahsoka.Application Shutdown (Exit)");
            Environment.Exit(0);
        }
    }
}
