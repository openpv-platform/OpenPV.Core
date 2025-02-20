using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Core;

/// <summary>
/// Internal Logging Object for Ahsoka Systems which allows 
/// messages to be filtered based on verbosity of the sender.
/// Configure for high verbosity to see all messages and low to only see warnings.
/// </summary>
public static class AhsokaLogging
{
    static CancellationTokenSource memoryCancelToken;

    /// <summary>
    /// Configures the internal logging for the specified level of verbosity.
    /// </summary>
    public static AhsokaVerbosity LoggingVerbosity { get; set; } = AhsokaVerbosity.Medium;

    /// <summary>
    /// Send a message with the specified verbosity.
    /// </summary>
    /// <param name="verbosity"></param>
    /// <param name="message"></param>
    public static void LogMessage(AhsokaVerbosity verbosity, string message)
    {
        if (verbosity >= (LoggingVerbosity & ~AhsokaVerbosity.Performance))
            Console.WriteLine(message);
    }

    /// <summary>
    /// Send a message with the specified verbosity.
    /// </summary>
    /// <param name="message"></param>
    internal static void LogStopwatchMessage(string message)
    {
        if (LoggingVerbosity.HasFlag(AhsokaVerbosity.Performance))
            Console.WriteLine(message);
    }

    /// <summary>
    /// Start Printing Memory Metrics to Log
    /// </summary>
    /// <param name="intervalInMs">How often to capture / print log</param>
    /// <param name="logVerbosity">Log Verbosity</param>
    public static void StartMemoryLog(int intervalInMs, AhsokaVerbosity logVerbosity)
    {
        memoryCancelToken = new CancellationTokenSource();
        Task.Factory.StartNew(() =>
        {
            while (!memoryCancelToken.IsCancellationRequested)
            {
                // Print Memory Metrics
                Process currentProc = Process.GetCurrentProcess();
                StringBuilder builder = new();
                builder.AppendLine();
                builder.AppendLine("---------Memory Usage Info----------");
                builder.AppendLine($"       GC Heap: {((double)GC.GetGCMemoryInfo().HeapSizeBytes) / 1024 / 1024:F2} MB");
                builder.AppendLine($"       Working Set: {((double)currentProc.WorkingSet64) / 1024 / 1024:F2} MB");
                builder.AppendLine("------------------------------------");
                builder.AppendLine();
                AhsokaLogging.LogMessage(logVerbosity, builder.ToString());

                Task.Delay(intervalInMs).Wait();
            }

        }, memoryCancelToken.Token);
    }

    /// <summary>
    /// Request Memory Log Stop
    /// </summary>
    public static void StopMemoryLog()
    {
        memoryCancelToken?.Cancel();
    }



}

/// <summary>
/// Level of Verbosity for Ahsoka Logging
/// </summary>
[Flags]
public enum AhsokaVerbosity
{
    /// <summary>
    /// Undocumented Internal Component Messages - Intended for Internal Developers Only
    /// </summary>
    Developer = 0,

    /// <summary>
    /// Low Priority Messages
    /// </summary>
    Low = 1,

    /// <summary>
    /// Warning Messages
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Important Messages and Errors
    /// </summary>
    High = 4, // All Log Messages

    /// <summary>
    /// Enable Performance Logging (Not intended to be assigned as message importance level)
    /// </summary>
    Performance = 8,
}