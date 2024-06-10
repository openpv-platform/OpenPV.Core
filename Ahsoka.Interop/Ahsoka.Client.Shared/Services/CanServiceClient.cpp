#include "CanServiceClient.h"
#include "CanServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaCAN;

namespace AhsokaCAN
{
    typedef AhsokaClientEndPoint<CanMessageTypes_Ids> CanService_t;
	
	CanServiceClient::CanServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<CanMessageTypes_Ids>(new CanService_t(AhsokaConfiguration::GetServiceConfiguration(CanServiceName), CanServiceMessages()))
    {
        
    }

    CanServiceClient::CanServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<CanMessageTypes_Ids>(new CanService_t(config, CanServiceMessages()))
    {
        
    }

	void CanServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<CanService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<CanMessageResult> CanServiceClient::SendCanMessages(CanMessageDataCollection& canMessageDataCollection, int timeoutInMs)
	{
		return GetEndPoint<CanService_t>()->SendNotificationWithResponse<CanMessageResult>(CanMessageTypes_Ids_SEND_CAN_MESSAGES, &canMessageDataCollection, timeoutInMs);
	}

	unique_ptr<CanMessageResult> CanServiceClient::SendRecurringCanMessage(RecurringCanMessage& recurringCanMessage, int timeoutInMs)
	{
		return GetEndPoint<CanService_t>()->SendNotificationWithResponse<CanMessageResult>(CanMessageTypes_Ids_SEND_RECURRING_CAN_MESSAGE, &recurringCanMessage, timeoutInMs);
	}

	void CanServiceClient::ApplyMessageFilter(ClientCanFilter& clientCanFilter, int timeoutInMs)
	{
		GetEndPoint<CanService_t>()->SendNotificationWithResponse(CanMessageTypes_Ids_APPLY_MESSAGE_FILTER, &clientCanFilter, timeoutInMs);
	}




	#if __has_include("CanServiceClientExtensions.cxx") 
	# include "CanServiceClientExtensions.cxx"
	#endif
}