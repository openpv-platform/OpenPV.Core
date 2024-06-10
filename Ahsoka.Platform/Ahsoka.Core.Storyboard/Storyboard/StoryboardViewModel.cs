using Ahsoka.Utility;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Ahsoka.Storyboard;

/// <summary>
/// Default Base Class for Customer Storyboard View Models
/// </summary>
public abstract class StoryboardViewModelBase : ViewModelBase
{
    #region Fields
    #endregion

    #region Properties

    /// <summary>
    /// Storyboard Message which this Model Object Manages
    /// </summary>
    public StoryboardMessage Message { get; init; }

    /// <summary>
    /// Name of the SBIO Event...should match Storyboard Event Name
    /// </summary>
    public string EventName { get; init; }


    #endregion

    #region Methods
    /// <summary>
    /// Constructor to Allocate Data (Serialize)
    /// </summary>
    /// <param name="eventName">Name of Storyboard Event</param>
    /// <param name="eventFormat">Description of Message Format</param>
    /// <param name="direction">Direction of Event In/Out</param>
    protected StoryboardViewModelBase(string eventName, string eventFormat, StoryboardMessageDirection direction)
    {
        EventName = eventName;
        var finalProp = GetMetadata().Last().Value;
        Message = new()
        {
            Name = EventName,
            Format = eventFormat,
            Direction = direction,
            Data = new byte[finalProp.StartByte + finalProp.ElementSize * finalProp.Count + (finalProp.Count == 0 ? 1 : 0)]
        };
    }

    /// <summary>
    /// Constructor to use Existing Data (Deserialize)
    /// </summary>
    /// <param name="message">Storyboard Message which this Model Object Manages</param>
    protected StoryboardViewModelBase(StoryboardMessage message)
    {
        Message = message;
        EventName = Message.Name;
    }

    /// <summary>
    /// Internal SetValue Method used to set a value and call the Property Changed with a Single Line of Code.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="newValue">New Value to be Set if it differs from the Base Value</param>
    /// <param name="memberName">Name of the Property or Member</param>
    protected void OnSetValue<T>(T newValue, [CallerMemberName] string memberName = "")
    {
        if (GetMetadata().TryGetValue(memberName, out StoryboardPropertyInfo info))
        {
            T baseValue = info.GetValue<T>(Message.Data);
            if (!baseValue.Equals(newValue))
            {
                var data = Message.Data;
                info.SetValue<T>(ref data, newValue);
                if (data.Length != Message.Data.Length)
                    Message.Data = data;
                OnPropertyChanged(memberName);
            }
        }
    }

    /// <summary>
    /// Internal GetValue Method used to get a value from the backing data store.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="memberName">Name of the Property or Member</param>
    /// <param name="scaled">Scale the Property Before Returning</param>
    protected T OnGetValue<T>([CallerMemberName] string memberName = "", bool scaled = true)
    {
        if (GetMetadata().TryGetValue(memberName, out StoryboardPropertyInfo info))
        {
            return info.GetValue<T>(Message.Data);
        }
        return default;
    }

    /// <summary>
    /// Method used to get Property Infos from Metadata
    /// </summary>
    public abstract Dictionary<string, StoryboardPropertyInfo> GetMetadata();
    #endregion
}

/// <summary>
/// Extension for Builder from Generic Base
/// </summary>
public static class StoryboardExtension
{
    /// <summary>
    /// Reference to Generated Metadata
    /// </summary>
    public static IStoryboardMetadata Metadata { get; set; }

    /// <summary>
    /// Creates a Specific Object from a Message
    /// </summary>
    /// <typeparam name="T">Base Class of Generated Object</typeparam>
    /// <param name="message">Storyboard Message with data for construction</param>
    public static T GetObject<T>(this StoryboardMessage message) where T : StoryboardViewModelBase
    {
        return (T)Metadata?.CreateObject(message);
    }
}
