using System;

namespace Ahsoka.Utility;

/// <summary>
/// Progress Object used when listening to progress from the same thread as the executer (mimics the Task Progress Interface using Invoke)
/// </summary>
/// <typeparam name="T"></typeparam>
public class LocalProgress<T> : IProgress<T>
{
    readonly Action<T> handler;

    /// <summary>
    /// Constructor taking an Action for Reporting Progress
    /// </summary>
    /// <param name="handler"></param>
    public LocalProgress(Action<T> handler)
    {
        this.handler = handler;
    }

    /// <summary>
    /// Report Progress back to Caller
    /// </summary>
    /// <param name="value">Progress Value passed to Report</param>
    public void Report(T value)
    {
        this.handler.Invoke(value);
    }
}
