#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaData
{
    class DataServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            DataServiceMessages()
            {
				RegisterServiceRequest(DataMessageTypes_Ids_SEARCH_AVAILABLE_KEYS, CreateCTOR<AhsokaData::AvailableKeySearchInfo>(), CreateCTOR<AhsokaData::KeyList>());
				RegisterServiceRequest(DataMessageTypes_Ids_GET_KEY_VALUES, CreateCTOR<AhsokaData::KeyList>(), CreateCTOR<AhsokaData::KeyValueList>());
				RegisterServiceRequest(DataMessageTypes_Ids_SET_KEY_VALUES, CreateCTOR<AhsokaData::KeyValueList>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(DataMessageTypes_Ids_SUBSCRIBE_TO_KEYS, CreateCTOR<AhsokaData::KeySubscriptionRequest>(), CreateCTOR<AhsokaData::KeySubscriberInfo>());
				RegisterServiceRequest(DataMessageTypes_Ids_UNSUBSCRIBE_TO_KEYS, CreateCTOR<AhsokaData::KeySubscriptionRequest>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(DataMessageTypes_Ids_NOTIFY_KEY_VALUE_CHANGED, CreateCTOR<AhsokaData::KeyValueNotification>());
				RegisterServiceNotification(DataMessageTypes_Ids_NOTIFY_KEY_ADDED, CreateCTOR<AhsokaData::KeyList>());

            }
    };
}