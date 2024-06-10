namespace Ahsoka.Dispatch;

/// <summary>
/// Accepts AutoStart / AutoStop
/// </summary>
public interface ICanAutoStart
{
    /// <summary>
    /// Stop any Source Threads
    /// </summary>
    /// <param name="requester"></param>
    public void AutoStop(object requester);

    /// <summary>
    /// Start any Source Threads
    /// </summary>
    /// <param name="requester"></param>
    public void AutoStart(object requester);
}