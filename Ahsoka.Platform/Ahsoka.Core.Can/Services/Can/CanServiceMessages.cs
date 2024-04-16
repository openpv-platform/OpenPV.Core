using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;
using System.Text;

namespace Ahsoka.Services.Can;

internal class CanServiceMessages : AhsokaMessagesBase
{
    public CanServiceMessages()
    {
        this.RegisterServiceRequest(CanMessageTypes.Ids.OpenCommunicationChannel, typeof(EmptyNotification), typeof(CanApplicationCalibration), false, false);
        this.RegisterServiceRequest(CanMessageTypes.Ids.CloseCommunicationChannel, typeof(EmptyNotification), typeof(EmptyNotification), false, false);

        this.RegisterServiceRequest(CanMessageTypes.Ids.SendCanMessages, typeof(CanMessageDataCollection), typeof(CanMessageResult));
        this.RegisterServiceRequest(CanMessageTypes.Ids.SendRecurringCanMessage, typeof(RecurringCanMessage), typeof(CanMessageResult));

        this.RegisterServiceNotification(CanMessageTypes.Ids.CanMessagesReceived, typeof(CanMessageDataCollection));
        this.RegisterServiceNotification(CanMessageTypes.Ids.NetworkStateChanged, typeof(CanState));

        // Add a Filter / Port For Clients
        this.RegisterServiceRequest(CanMessageTypes.Ids.ApplyMessageFilter, typeof(ClientCanFilter), typeof(EmptyNotification));

        // Coprocessor Registration
        this.RegisterServiceNotification(CanMessageTypes.Ids.CanServiceIsReadyNotification, typeof(EmptyNotification));
        this.RegisterServiceNotification(CanMessageTypes.Ids.CoprocessorIsReadyNotification, typeof(EmptyNotification), false, true);
        this.RegisterServiceRequest(CanMessageTypes.Ids.CoprocessorHeartbeat, typeof(EmptyNotification), typeof(EmptyNotification), false, true);
    }

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("CanServiceClientExtensions.h", UTF8Encoding.UTF8.GetBytes(Properties.CANResources.CanServiceClientExtensionsH));
        result.Add("IHasCanData.h", UTF8Encoding.UTF8.GetBytes(Properties.CANResources.IHasCanData));
        result.Add("CanServiceClientIncludes.h", UTF8Encoding.UTF8.GetBytes(Properties.CANResources.CanServiceClientIncludes));

        if (includeImplementation) 
            result.Add("CanServiceClientExtensions.cxx", UTF8Encoding.UTF8.GetBytes(Properties.CANResources.CanServiceClientExtensionsCXX));

        return result;
    }
}

