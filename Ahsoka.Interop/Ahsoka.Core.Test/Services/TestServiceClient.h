#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaConfiguration.h"
#include "AhsokaClientEndPoint.h"
#include "Services.pb.h"
#include "TestServiceMessages.h"

using namespace std;
using namespace AhsokaSystem;
using namespace AhsokaServiceFramework;

namespace AhsokaSystem
{
	class TestServiceClient : public AhsokaServiceFramework::AhsokaClientEndPoint<TestMessageTypes>
	{
		public:

			TestServiceClient(ServiceConfiguration config):
				AhsokaServiceFramework::AhsokaClientEndPoint<TestMessageTypes>(config, TestServiceMessages()) {}

			unique_ptr<AhsokaSystem::ServiceConfiguration> EchoConfig(AhsokaSystem::ServiceConfiguration& configValue, int timeoutInMs)
			{
				return SendMessageWithResponse<AhsokaSystem::ServiceConfiguration>(EchoTest, &configValue, timeoutInMs);
			}
			
	};
}