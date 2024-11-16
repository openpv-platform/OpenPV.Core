#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaConfiguration.h"
#include "AhsokaServiceEndPoint.h"
#include "TestServiceMessages.h"

using namespace std;
using namespace AhsokaSystem;
using namespace AhsokaServiceFramework;

namespace AhsokaSystem
{
	class TestService : public AhsokaServiceFramework::AhsokaServiceEndPoint<TestMessageTypes>
	{
		public:

			TestService(ServiceConfiguration config):
				AhsokaServiceFramework::AhsokaServiceEndPoint<TestMessageTypes>(config, TestServiceMessages()) 
			{

			}	
			
			void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message)
			{
				if (messageHeader.transport_id() == EchoTest)
				{
					SendNotificationInternal(messageHeader, &message, false);

					messageHeader.set_transport_id(EchoTestNotification);
					SendNotificationInternal(messageHeader, 0, true);
				}
			}
	};
}