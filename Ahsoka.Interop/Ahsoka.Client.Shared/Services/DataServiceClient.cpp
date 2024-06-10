#include "DataServiceClient.h"
#include "DataServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaData;

namespace AhsokaData
{
    typedef AhsokaClientEndPoint<DataMessageTypes_Ids> DataService_t;
	
	DataServiceClient::DataServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<DataMessageTypes_Ids>(new DataService_t(AhsokaConfiguration::GetServiceConfiguration(DataServiceName), DataServiceMessages()))
    {
        
    }

    DataServiceClient::DataServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<DataMessageTypes_Ids>(new DataService_t(config, DataServiceMessages()))
    {
        
    }

	void DataServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<DataService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<KeyList> DataServiceClient::SearchAvailableKeys(AvailableKeySearchInfo& availableKeySearchInfo, int timeoutInMs)
	{
		return GetEndPoint<DataService_t>()->SendNotificationWithResponse<KeyList>(DataMessageTypes_Ids_SEARCH_AVAILABLE_KEYS, &availableKeySearchInfo, timeoutInMs);
	}

	unique_ptr<KeyValueList> DataServiceClient::GetKeyValues(KeyList& keyList, int timeoutInMs)
	{
		return GetEndPoint<DataService_t>()->SendNotificationWithResponse<KeyValueList>(DataMessageTypes_Ids_GET_KEY_VALUES, &keyList, timeoutInMs);
	}

	void DataServiceClient::SetKeyValues(KeyValueList& keyValueList, int timeoutInMs)
	{
		GetEndPoint<DataService_t>()->SendNotificationWithResponse(DataMessageTypes_Ids_SET_KEY_VALUES, &keyValueList, timeoutInMs);
	}

	unique_ptr<KeySubscriberInfo> DataServiceClient::SubscribeToKeys(KeySubscriptionRequest& keySubscriptionRequest, int timeoutInMs)
	{
		return GetEndPoint<DataService_t>()->SendNotificationWithResponse<KeySubscriberInfo>(DataMessageTypes_Ids_SUBSCRIBE_TO_KEYS, &keySubscriptionRequest, timeoutInMs);
	}

	void DataServiceClient::UnsubscribeToKeys(KeySubscriptionRequest& keySubscriptionRequest, int timeoutInMs)
	{
		GetEndPoint<DataService_t>()->SendNotificationWithResponse(DataMessageTypes_Ids_UNSUBSCRIBE_TO_KEYS, &keySubscriptionRequest, timeoutInMs);
	}




	#if __has_include("DataServiceClientExtensions.cxx") 
	# include "DataServiceClientExtensions.cxx"
	#endif
}