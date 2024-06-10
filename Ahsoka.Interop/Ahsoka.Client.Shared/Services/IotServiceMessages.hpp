#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaIot
{
    class IotServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            IotServiceMessages()
            {
				RegisterServiceRequest(IotMessageTypes_Ids_CREATE_CLIENT, CreateCTOR<AhsokaIot::ClientInfo>(), CreateCTOR<AhsokaIot::ClientConnectionInfo>());
				RegisterServiceRequest(IotMessageTypes_Ids_DESTROY_CLIENT, CreateCTOR<AhsokaIot::ClientMessage>(), CreateCTOR<AhsokaIot::ClientConnectionInfo>());
				RegisterServiceRequest(IotMessageTypes_Ids_SUBSCRIBE_TOPIC, CreateCTOR<AhsokaIot::TopicSubscription>(), CreateCTOR<AhsokaIot::TopicResult>());
				RegisterServiceRequest(IotMessageTypes_Ids_UNSUBSCRIBE_TOPIC, CreateCTOR<AhsokaIot::TopicSubscription>(), CreateCTOR<AhsokaIot::TopicResult>());
				RegisterServiceRequest(IotMessageTypes_Ids_PUBLISH_TOPIC, CreateCTOR<AhsokaIot::TopicMessage>(), CreateCTOR<AhsokaIot::TopicResult>());
				RegisterServiceNotification(IotMessageTypes_Ids_TOPIC_RECEIVED_NOTIFICATION, CreateCTOR<AhsokaIot::TopicMessage>());

            }
    };
}