#include "IotServiceClient.h"
#include "IotServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaIot;

namespace AhsokaIot
{
    typedef AhsokaClientEndPoint<IotMessageTypes_Ids> IotService_t;
	
	IotServiceClient::IotServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<IotMessageTypes_Ids>(new IotService_t(AhsokaConfiguration::GetServiceConfiguration(IotServiceName), IotServiceMessages()))
    {
        
    }

    IotServiceClient::IotServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<IotMessageTypes_Ids>(new IotService_t(config, IotServiceMessages()))
    {
        
    }

	void IotServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<IotService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<ClientConnectionInfo> IotServiceClient::CreateClient(ClientInfo& clientInfo, int timeoutInMs)
	{
		return GetEndPoint<IotService_t>()->SendNotificationWithResponse<ClientConnectionInfo>(IotMessageTypes_Ids_CREATE_CLIENT, &clientInfo, timeoutInMs);
	}

	unique_ptr<ClientConnectionInfo> IotServiceClient::DestroyClient(ClientMessage& clientMessage, int timeoutInMs)
	{
		return GetEndPoint<IotService_t>()->SendNotificationWithResponse<ClientConnectionInfo>(IotMessageTypes_Ids_DESTROY_CLIENT, &clientMessage, timeoutInMs);
	}

	unique_ptr<TopicResult> IotServiceClient::SubscribeTopic(TopicSubscription& topicSubscription, int timeoutInMs)
	{
		return GetEndPoint<IotService_t>()->SendNotificationWithResponse<TopicResult>(IotMessageTypes_Ids_SUBSCRIBE_TOPIC, &topicSubscription, timeoutInMs);
	}

	unique_ptr<TopicResult> IotServiceClient::UnsubscribeTopic(TopicSubscription& topicSubscription, int timeoutInMs)
	{
		return GetEndPoint<IotService_t>()->SendNotificationWithResponse<TopicResult>(IotMessageTypes_Ids_UNSUBSCRIBE_TOPIC, &topicSubscription, timeoutInMs);
	}

	unique_ptr<TopicResult> IotServiceClient::PublishTopic(TopicMessage& topicMessage, int timeoutInMs)
	{
		return GetEndPoint<IotService_t>()->SendNotificationWithResponse<TopicResult>(IotMessageTypes_Ids_PUBLISH_TOPIC, &topicMessage, timeoutInMs);
	}




	#if __has_include("IotServiceClientExtensions.cxx") 
	# include "IotServiceClientExtensions.cxx"
	#endif
}