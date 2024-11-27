using System;

namespace Ahsoka.ServiceFramework;

public abstract partial class AhsokaClientBase<TTransportIds>
{
    /// <summary>
    /// Send Message and Wait for Response 
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    /// <param name="timeoutInMs"></param>
    [Obsolete("Use SendMessageWithResponse instead")]
    public void SendNotificationWithResponse(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse<EmptyNotification>(transportId, requestObject, timeoutInMs);
    }

    /// <summary>
    /// Send Message and Wait for Response 
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    /// <param name="timeoutInMs"></param>
    [Obsolete("Use SendMessageWithResponse instead")]
    public virtual T SendNotificationWithResponse<T>(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue) where T : class
    {
        return SendMessageWithResponse<T>(transportId, requestObject, timeoutInMs);
    }
}
