namespace Ahsoka.System;

/// <summary>
///  Interface indicating that the object can receive a cancel request
/// </summary>
public interface ICanCancel
{
    /// <summary>
    /// Send Cancel Request to the Implmenting Object
    /// </summary>
    void Cancel();
}