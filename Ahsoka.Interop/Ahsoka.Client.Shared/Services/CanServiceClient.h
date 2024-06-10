#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaCAN;

#if __has_include("CanServiceClientIncludes.h") 
# include "CanServiceClientIncludes.h"
#endif

namespace AhsokaCAN
{
	const std::string CanServiceName = "CanService";

	class CanServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<CanMessageTypes_Ids>
	{
		public:

			CanServiceClient();
			CanServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: CanMessageTypes_Ids_CAN_MESSAGES_RECEIVED MessageType: unique_ptr<CanMessageDataCollection>
			// TransportID: CanMessageTypes_Ids_NETWORK_STATE_CHANGED MessageType: unique_ptr<CanState>
			// TransportID: CanMessageTypes_Ids_CAN_SERVICE_IS_READY_NOTIFICATION MessageType: void

			unique_ptr<CanMessageResult> SendCanMessages(CanMessageDataCollection& canMessageDataCollection, int timeoutInMs = INT_MAX);
			unique_ptr<CanMessageResult> SendRecurringCanMessage(RecurringCanMessage& recurringCanMessage, int timeoutInMs = INT_MAX);
			void ApplyMessageFilter(ClientCanFilter& clientCanFilter, int timeoutInMs = INT_MAX);


			#if __has_include("CanServiceClientExtensions.h") 
			# include "CanServiceClientExtensions.h"
			#endif
	};
}