#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaCAN
{
    class CanServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            CanServiceMessages()
            {
				RegisterServiceRequest(CanMessageTypes_Ids_OPEN_COMMUNICATION_CHANNEL, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaCAN::CanApplicationCalibration>());
				RegisterServiceRequest(CanMessageTypes_Ids_CLOSE_COMMUNICATION_CHANNEL, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(CanMessageTypes_Ids_SEND_CAN_MESSAGES, CreateCTOR<AhsokaCAN::CanMessageDataCollection>(), CreateCTOR<AhsokaCAN::CanMessageResult>());
				RegisterServiceRequest(CanMessageTypes_Ids_SEND_RECURRING_CAN_MESSAGE, CreateCTOR<AhsokaCAN::RecurringCanMessage>(), CreateCTOR<AhsokaCAN::CanMessageResult>());
				RegisterServiceNotification(CanMessageTypes_Ids_CAN_MESSAGES_RECEIVED, CreateCTOR<AhsokaCAN::CanMessageDataCollection>());
				RegisterServiceNotification(CanMessageTypes_Ids_NETWORK_STATE_CHANGED, CreateCTOR<AhsokaCAN::CanState>());
				RegisterServiceRequest(CanMessageTypes_Ids_APPLY_MESSAGE_FILTER, CreateCTOR<AhsokaCAN::ClientCanFilter>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(CanMessageTypes_Ids_CAN_SERVICE_IS_READY_NOTIFICATION, CreateCTOR<AhsokaServiceFramework::EmptyNotification>());

            }
    };
}