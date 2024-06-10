using System;

namespace Ahsoka.Storyboard;

/// <summary>
/// A set of string enumerations that define the direction of the Storyboard messages
/// </summary>
public enum StoryboardMessageDirection
{
    /// <summary>
    /// The Storyboard Message is coming into the C# application.
    /// </summary>
    In,
    /// <summary>
    /// The Storyboard Message is coming out from the C# application.
    /// </summary>
    Out,
}

/// <summary>
/// A static class for containing extension methods
/// </summary>
public static class StoryboardMessageExtensions
{
    /// <summary>
    /// Utility function used to Extract a Type Safe Object from SBIO
    /// </summary>
    internal static string GetMessageDirectionString(this StoryboardMessageDirection directionEnum)
    {
        return directionEnum switch
        {
            StoryboardMessageDirection.In => "0x20",
            StoryboardMessageDirection.Out => "0x40",
            _ => throw new Exception("The direction provided isn't supported."),
        };
    }

}

/// <summary>
/// General Purpose object for managing Storyboard Messages (events) for the SBIO Interface
/// </summary>
public class StoryboardMessage
{
    /// <summary>
    /// Target Object in Storyboard
    /// </summary>
    public string Target { get; private set; }

    /// <summary>
    /// The SBIO Event Attribute
    /// </summary>
    public SBMessageEventName EventAttribute { get; private set; }

    /// <summary>
    /// Format of the SBIO Message (See Storyboard Docs for Details)
    /// </summary>
    public string Format { get; private set; }

    /// <summary>
    /// Data being sent / received over SBIO.
    /// </summary>
    public byte[] Data { get; private set; }

    /// <summary>
    /// Utility function used to Extract a Type Safe Object from SBIO
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetObject<T>() where T : class, new()
    {
        return GetObject() as T;
    }

    /// <summary>
    /// Gets the Object from the message without the type.
    /// </summary>
    /// <returns></returns>
    public object GetObject()
    {
        var handler = StoryboardMessageSerializer.GetMessageHandler(EventAttribute.EventName);
        return handler.DataToObject(Data);
    }

    /// <summary>
    /// Construct a new message to send over SBIO.
    /// </summary>
    /// <param name="name">Name of the Event</param>
    /// <param name="format">Format of the Event</param>
    /// <param name="direction">Direction of the Event</param>
    /// <param name="data">Data for the Event</param>
    /// <param name="target">Target Object in Storyboard</param>
    public StoryboardMessage(string name, string format, string direction, byte[] data, string target = null)
    {
        Target = target;
        EventAttribute = 
            new() {
            EventName = name,
            EventDirection = direction,
        };
        Format = format;
        Data = data;
    }

    /// <summary>
    /// Create a messate from a strongly typed Storyboard Event Object.
    /// </summary>
    /// <param name="messageObject"></param>
    /// <param name="target"></param>
    public StoryboardMessage(object messageObject, string target = null)
    {
        var handler = StoryboardMessageSerializer.GetMessageHandler(messageObject.GetType());

        Target = target;
        EventAttribute = handler.EventAttribute;
        Format = handler.Format;

        // Translate Object to Data
        Data = handler.ObjectToData(messageObject);
    }
}
