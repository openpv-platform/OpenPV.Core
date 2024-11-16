#pragma once

#include <string>
#include <functional>
#include "AhsokaConfiguration.h"

namespace AhsokaSystem
{
    enum TestMessageTypes : int
    {
        EchoTest = 1,
        EchoTestNotification = 2
    };

    const std::string TestServiceName = "TestService";


    class TestServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            TestServiceMessages()
            {
                RegisterServiceRequest(EchoTest, CreateCTOR<AhsokaSystem::ServiceConfiguration>(), CreateCTOR<AhsokaSystem::ServiceConfiguration>());
		        RegisterServiceNotification(EchoTestNotification, CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
			}
    };
}