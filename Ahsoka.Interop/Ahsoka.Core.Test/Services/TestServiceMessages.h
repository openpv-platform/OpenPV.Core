#pragma once

#include <string>
#include <functional>
#include "AhsokaConfiguration.h"

namespace AhsokaCore
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
                RegisterServiceRequest(EchoTest, CreateCTOR<AhsokaCore::ServiceConfiguration>(), CreateCTOR<AhsokaCore::ServiceConfiguration>());
		        RegisterServiceNotification(EchoTestNotification, CreateCTOR<AhsokaCore::ServiceConfiguration>());
			}
    };
}