using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using Mono.Unix;
using Mono.Unix.Native;
using System;
using System.Threading;

namespace Ahsoka.System;

/// <summary>
/// Application Context object that allows the Application to block
/// waiting on a linux or windows signals.
/// </summary>
public static class ApplicationContext
{
    static readonly ManualResetEventSlim reset = new();

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
        AhsokaRuntime.Default.DisconnectAllEndPoints();

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
