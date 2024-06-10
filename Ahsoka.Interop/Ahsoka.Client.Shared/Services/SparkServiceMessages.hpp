#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaSpark
{
    class SparkServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            SparkServiceMessages()
            {
				RegisterServiceRequest(SparkMessageTypes_Ids_SET_SPARK_SUBSCRIPTION, CreateCTOR<AhsokaSpark::SparkSubscription>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(SparkMessageTypes_Ids_GET_SPARK_SUBSCRIPTION, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSpark::SparkSubscriptionInfo>());
				RegisterServiceRequest(SparkMessageTypes_Ids_GET_AVAILABLE_RELEASES, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSpark::SparkReleaseInfo>());
				RegisterServiceRequest(SparkMessageTypes_Ids_GET_RELEASE, CreateCTOR<AhsokaSpark::SparkReleaseDownloadRequest>(), CreateCTOR<AhsokaSpark::SparkReleaseDownloadCompleted>());
				RegisterServiceNotification(SparkMessageTypes_Ids_GET_RELEASE_NOTIFICATION, CreateCTOR<AhsokaSpark::SparkReleaseDownloadNotification>());

            }
    };
}