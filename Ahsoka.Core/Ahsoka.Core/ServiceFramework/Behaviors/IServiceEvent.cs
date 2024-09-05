using System;
using System.Collections.Generic;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Base Interface for adding Event Behaviors to Ahsoka Endpoint Clients
/// </summary>
public interface IServiceEvent
{
    /// <summary>
    /// Default Implementation used by All Services
    /// </summary>
    static IServiceEvent Default
    {
        get { return defaultEvent; }
        set
        {
            if (defaultEvent != null && value != null)
                throw new InvalidOperationException("The Default Event has been set and can not be re-assigned");

            defaultEvent = value;
        }
    }

    private static IServiceEvent defaultEvent;


    /// <summary>
    /// Send an Event to the Service
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    int SendEvent(EventData eventData, EventState state);
}
