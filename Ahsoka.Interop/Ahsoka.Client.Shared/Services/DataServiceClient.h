#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaData;

#if __has_include("DataServiceClientIncludes.h") 
# include "DataServiceClientIncludes.h"
#endif

namespace AhsokaData
{
	const std::string DataServiceName = "DataService";

	class DataServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<DataMessageTypes_Ids>
	{
		public:

			DataServiceClient();
			DataServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: DataMessageTypes_Ids_NOTIFY_KEY_VALUE_CHANGED MessageType: unique_ptr<KeyValueNotification>
			// TransportID: DataMessageTypes_Ids_NOTIFY_KEY_ADDED MessageType: unique_ptr<KeyList>

			unique_ptr<KeyList> SearchAvailableKeys(AvailableKeySearchInfo& availableKeySearchInfo, int timeoutInMs = INT_MAX);
			unique_ptr<KeyValueList> GetKeyValues(KeyList& keyList, int timeoutInMs = INT_MAX);
			void SetKeyValues(KeyValueList& keyValueList, int timeoutInMs = INT_MAX);
			unique_ptr<KeySubscriberInfo> SubscribeToKeys(KeySubscriptionRequest& keySubscriptionRequest, int timeoutInMs = INT_MAX);
			void UnsubscribeToKeys(KeySubscriptionRequest& keySubscriptionRequest, int timeoutInMs = INT_MAX);


			#if __has_include("DataServiceClientExtensions.h") 
			# include "DataServiceClientExtensions.h"
			#endif
	};
}