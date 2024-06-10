#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaIot;

#if __has_include("IotServiceClientIncludes.h") 
# include "IotServiceClientIncludes.h"
#endif

namespace AhsokaIot
{
	const std::string IotServiceName = "IotService";

	class IotServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<IotMessageTypes_Ids>
	{
		public:

			IotServiceClient();
			IotServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: IotMessageTypes_Ids_TOPIC_RECEIVED_NOTIFICATION MessageType: unique_ptr<TopicMessage>

			unique_ptr<ClientConnectionInfo> CreateClient(ClientInfo& clientInfo, int timeoutInMs = INT_MAX);
			unique_ptr<ClientConnectionInfo> DestroyClient(ClientMessage& clientMessage, int timeoutInMs = INT_MAX);
			unique_ptr<TopicResult> SubscribeTopic(TopicSubscription& topicSubscription, int timeoutInMs = INT_MAX);
			unique_ptr<TopicResult> UnsubscribeTopic(TopicSubscription& topicSubscription, int timeoutInMs = INT_MAX);
			unique_ptr<TopicResult> PublishTopic(TopicMessage& topicMessage, int timeoutInMs = INT_MAX);


			#if __has_include("IotServiceClientExtensions.h") 
			# include "IotServiceClientExtensions.h"
			#endif
	};
}