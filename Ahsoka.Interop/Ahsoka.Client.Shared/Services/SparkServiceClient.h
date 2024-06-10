#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaSpark;

#if __has_include("SparkServiceClientIncludes.h") 
# include "SparkServiceClientIncludes.h"
#endif

namespace AhsokaSpark
{
	const std::string SparkServiceName = "SparkService";

	class SparkServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<SparkMessageTypes_Ids>
	{
		public:

			SparkServiceClient();
			SparkServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: SparkMessageTypes_Ids_GET_RELEASE_NOTIFICATION MessageType: unique_ptr<SparkReleaseDownloadNotification>

			void SetSparkSubscription(SparkSubscription& sparkSubscription, int timeoutInMs = INT_MAX);
			unique_ptr<SparkSubscriptionInfo> GetSparkSubscription(int timeoutInMs = INT_MAX);
			unique_ptr<SparkReleaseInfo> GetAvailableReleases(int timeoutInMs = INT_MAX);
			unique_ptr<SparkReleaseDownloadCompleted> GetRelease(SparkReleaseDownloadRequest& sparkReleaseDownloadRequest, int timeoutInMs = INT_MAX);


			#if __has_include("SparkServiceClientExtensions.h") 
			# include "SparkServiceClientExtensions.h"
			#endif
	};
}