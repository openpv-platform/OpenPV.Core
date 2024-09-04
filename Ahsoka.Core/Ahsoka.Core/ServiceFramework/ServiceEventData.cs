using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Collection of Known Event Info for a Service
/// </summary>
public class SystemEventData : List<EventData>
{
    /// <summary>
    /// Name of the System
    /// </summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Assign Service and Keys to All Objects
    /// </summary>
    /// <param name="service"></param>
    public void AssignKeys(string service = null)
    {
        if (service != null)
            SystemName = service;

        foreach (var parameter in this)
        {
            if (parameter.SystemUniqueID == 0)
            {
                // Generate Key (32bit Hash
                var value = EventData.GenerateUniqueID(SystemName, parameter.ObjectName, parameter.Name);
                while (this.Any(x => x.SystemUniqueID == value))
                    value++;

                parameter.SystemUniqueID = value;
            }
        }
    }
}

/// <summary>
/// Information about a Parameter
/// </summary>
public class EventData
{
    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public uint SystemUniqueID { get; set; }

    /// <summary>
    /// Name of the Object (Optional)
    /// </summary>
    public string ObjectName { get; set; }

    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Status of the event
    /// </summary>
    public EventState State { get; set; }

    /// <summary>
    /// Event Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Generate a Unique ID For an System / Object / Property
    /// </summary>
    /// <param name="systemName"></param>
    /// <param name="objectName"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static uint GenerateUniqueID(string systemName, string objectName, string name)
    {
        return (uint)HashCode32.GetHashCode32($"{systemName}_{objectName}_{name}");
    }
}

/// <summary>
/// Event Status States
/// </summary>
public enum EventState
{
    /// <summary>
    /// No Event Data
    /// </summary>
    None = 0,
    /// <summary>
    /// Event has passed
    /// </summary>
    Passed = 1,
    /// <summary>
    /// Event in early pass stage
    /// </summary>
    Prepassed = 2,
    /// <summary>
    /// Event in early fail stage
    /// </summary>
    Prefailed = 3,
    /// <summary>
    /// Event has failed
    /// </summary>
    Failed = 4
}
