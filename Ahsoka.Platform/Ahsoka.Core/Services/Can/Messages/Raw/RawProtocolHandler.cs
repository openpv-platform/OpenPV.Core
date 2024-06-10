using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Messages;
internal class RawProtocolHandler : BaseProtocolHandler
{

    internal RawProtocolHandler(CanHandler messageHandler, CanServiceImplementation service)
        : base(messageHandler, service)
    {

    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null)
            return false;

        return Service.PortConfig.MessageConfiguration.Messages.Any(x => x.MessageType == MessageType.RawStandardFrame || x.MessageType == MessageType.RawExtendedFrame);
    }

    protected override bool ProtocolResponsible(CanMessageData messageData)
    {
        if (!GetAvailableMessage(messageData.Id, out var messageInfo))
            return false;

        if (messageInfo.Message.MessageType == MessageType.RawStandardFrame || messageInfo.Message.MessageType == MessageType.RawExtendedFrame)
            return true;

        return false;
    }

}
