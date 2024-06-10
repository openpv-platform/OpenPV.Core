using Ahsoka.ServiceFramework;
using Ahsoka.Services.Demo.Service;

namespace Ahsoka.Services.Demo;

/// <summary>
/// Clock Time Service Client demonstrating how to Create a Simple 
/// Ahsoka Service that allows Setting and Getting the time.  Here we use a generic template
/// that provides the socket handling and queuing needed for an Ahsoka Service
/// </summary>
public class TimeServiceClient : AhsokaClientBase<TimeServiceMessageTypes.Ids>
{
    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// This constructor requires that we pass in information about location 
    /// of the service and client.
    /// 
    /// Note that we are using a "Default" Configuration 'TimeServiceConfig' however
    /// these values could be stored in a file, specified in code or fetched from a central
    /// naming service in more complex architectures.
    /// </summary>
    /// <param name="config"></param>
    public TimeServiceClient() :
        base(TimeServiceConfig.Default, new TimeServiceMessages())
    {

    }

    /// <summary>
    /// This function creates a service to match the client.  For simple scenarios where the
    /// service is in the same process, this allows the client to directly start the service
    /// without needing to start it separately.
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new TimeService();
    }

    /// <summary>
    /// Return the Current Time from the Service
    /// </summary>
    /// <returns>TimeInfo</returns>
    public TimeInfo GetTime()
    {
        // This is a utility method in the base client that allows us to send a message and wait for a result.
        // the client will wait for <TimeInfo> and return that making it appear our async calls are sync calls
        // to the client caller.  For purely reactive style clients this could be left off and the internal
        // SendMessageInternal() could be used instead.
        return SendNotificationWithResponse<TimeInfo>(TimeServiceMessageTypes.Ids.GetTime);
    }

    /// <summary>
    /// Requests that the service sets the current time to match TimeInfo
    /// </summary>
    /// <param name="timeInfo">The Time to Set</param>
    public void SetTime(TimeInfo timeInfo)
    {
        // Because we return an empty notifcation there is no need to capture the result...we simply block until the result is returned
        // and return void.
        SendNotificationWithResponse(TimeServiceMessageTypes.Ids.SetTime, timeInfo);
    }
}

