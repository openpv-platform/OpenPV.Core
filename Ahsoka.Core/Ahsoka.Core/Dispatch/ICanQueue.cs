namespace Ahsoka.Core.Dispatch;

/// <summary>
/// Accepts AutoStart / AutoStop
/// </summary>
public interface ICanQueue
{
    /// <summary>
    /// Queues a message
    /// </summary>
    /// <param name="message"></param>
    public void Enqueue(object message);
}