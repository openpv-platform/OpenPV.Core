
using Ahsoka.ServiceFramework;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ahsoka.Utility;

/// <summary>
/// Creates a Stopwatch and Context for Tracing Time in an Ahsoka Process
/// Only logs if AhsokaLogging contains PerformanceLogging Flag.
/// </summary>
public class AhsokaStopwatch : IDisposable
{
    static readonly Stopwatch startTime = new();

    readonly bool isEnabled = true;
    readonly int myDepth = 0;
    static int currentDepth = 0;
    readonly Stopwatch watch = new();
    readonly string watchName = string.Empty;

    /// <summary>
    ///  Maximum Nesting Depth when Logging
    /// </summary>
    public static int MaxDepth { get; set; } = int.MaxValue;

    static AhsokaStopwatch()
    {
        startTime.Start();
    }

    /// <summary>
    /// Specify the name of the watch / context and intent level
    /// </summary>
    /// <param name="watchName">Watch Name / Context Name</param>
    public AhsokaStopwatch([CallerMemberName] string watchName = "")
    {
        this.watchName = watchName;
        watch.Start();

        myDepth = ++currentDepth;

        isEnabled = (myDepth <= MaxDepth);

        if (isEnabled)
            AhsokaLogging.LogStopwatchMessage($"{"".PadLeft(myDepth * 3)} {watchName} Started at {startTime.ElapsedMilliseconds}ms");

    }

    /// <summary>
    /// Shut down logging
    /// </summary>
    public void Dispose()
    {
        if (watch.IsRunning)
            StopTiming("");
    }

    /// <summary>
    /// Reset Timing to 0 and Continue Tracking Time.
    /// </summary>
    /// <param name="description"></param>
    public void ResetTiming(string description)
    {
        watch.Stop();

        if (isEnabled)
            AhsokaLogging.LogStopwatchMessage($"{"".PadLeft(myDepth * 3)} {watchName}:{description} Finished In: {watch.ElapsedMilliseconds}ms @ {startTime.ElapsedMilliseconds}ms");

        watch.Reset();
        watch.Start();
    }

    /// <summary>
    ///  Stop Timing with Custom Timing Message.
    /// </summary>
    /// <param name="description"></param>
    public void StopTiming(string description)
    {
        currentDepth--;
        watch.Stop();

        if (isEnabled)
            AhsokaLogging.LogStopwatchMessage($"{"".PadLeft(myDepth * 3)} {watchName}:{description} Finished In: {watch.ElapsedMilliseconds}ms @ {startTime.ElapsedMilliseconds}ms");
    }
}
