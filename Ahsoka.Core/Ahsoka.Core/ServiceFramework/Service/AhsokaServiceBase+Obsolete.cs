using Ahsoka.Utility;
using System;
using System.Linq;

namespace Ahsoka.ServiceFramework;

public abstract partial class AhsokaServiceBase<TTransportIds>
{
    /// <summary>
    /// Legacy Service Handler Message
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    [Obsolete]
    protected virtual void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {

    }

    /// <summary>
    /// Handle Messages and Return Result Messages
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    protected virtual partial void OnHandleServiceRequest(AhsokaServiceRequest message)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "A Legacy Service Request Was Recieved and Routed to OnHandleRecieve, Override OnHandleService Request to Elminate this Message.");
        #pragma warning disable CS0612 // Type or member is obsolete
        OnHandleReceive(message.Header, message.Message);
        #pragma warning restore CS0612 // Type or member is obsolete
    }

    /// <summary>
    /// Obsolete Send Message Function, Use SendNotification and SendResponse instead
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="messageObject"></param>
    /// <param name="isBroadcast"></param>
    [Obsolete]
    protected void SendMessageInternal(AhsokaMessageHeader messageHeader, object messageObject = null, bool isBroadcast = false)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "A Legacy Service Message Was Send using SendMessageInternal, use SendNotification and SendResponse instead.");
        if (isBroadcast)
            SendNotification(messageHeader.TransportId.IntToEnum<TTransportIds>(), messageObject);
        else
            SendResponse(new AhsokaServiceRequest() { Header = messageHeader, Message = messageObject, Socket = this.handledSockets.First() });
    }

    /// <summary>
    /// Legacy Reply Message Factory
    /// </summary>
    /// <param name="header"></param>
    /// <param name="transportId"></param>
    /// <returns></returns>
    [Obsolete]
    protected AhsokaMessageHeader CreateReplyMessageHeader(AhsokaMessageHeader header, TTransportIds transportId)
    {
        return new AhsokaMessageHeader()
        {
            TransportId = transportId.EnumToInt(),
            ClientMessageId = header.ClientMessageId,
            ClientId = header.ClientId,
        };
    }
}