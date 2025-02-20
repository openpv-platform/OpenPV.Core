using System;

namespace Ahsoka.Core.Dispatch;

/// <summary>
/// Interface for Input Based Model Callbacks
/// </summary>
public interface IDispatchSource
{
    /// <summary>
    /// Event to Notify
    /// </summary>
    public event EventHandler<EventArgs> HandleDispatchMessage;
}