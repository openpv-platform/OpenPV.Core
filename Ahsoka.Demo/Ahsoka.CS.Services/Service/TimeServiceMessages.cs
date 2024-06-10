using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Demo;


/// <summary>
/// This object is a dictionary of metadata about the messages our Ahsoka Service handle. It allows the underlying framework to 
/// decode the transport framing and translate it to the correct message for each message pair or notification.  This object
/// is shared by both the client and service.
/// </summary>
internal class TimeServiceMessages : AhsokaMessagesBase
{
    public TimeServiceMessages()
    {
        // Here we define a message that Takes Empty Notification (used when no data is needed) and returns the Current TimeInfo.
        this.RegisterServiceRequest(TimeServiceMessageTypes.Ids.GetTime, typeof(EmptyNotification), typeof(TimeInfo));

        // Here we define a message the Takes Time Info and returns an Empty Notification (ask) to let the client know the Set Time work has begun..
        this.RegisterServiceRequest(TimeServiceMessageTypes.Ids.SetTime, typeof(TimeInfo), typeof(EmptyNotification));

        // Here we define a notification (Service -> Client) to let the client application know that the Time was Set and the result.
        this.RegisterServiceNotification(TimeServiceMessageTypes.Ids.SetTimeCompleteNotification, typeof(SetTimeResult));
    }
}

