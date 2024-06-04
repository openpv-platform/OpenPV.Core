#pragma once

#include <string>
#include <functional>
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaConfiguration.h"
#include "AhsokaMessagesBase.h"

namespace AhsokaSystem
{
    class TestServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            TestServiceMessages()
            {
				RegisterServiceRequest(1, CreateCTOR<AhsokaSystem::EchoValue>(), CreateCTOR<AhsokaSystem::EchoValue>());
            }
    };
}