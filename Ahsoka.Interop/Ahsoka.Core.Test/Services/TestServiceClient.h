#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaConfiguration.h"
#include "AhsokaClientEndPoint.h"
#include "Services.pb.h"
#include "TestServiceMessages.h"

using namespace std;
using namespace AhsokaCore;
using namespace AhsokaServiceFramework;

namespace AhsokaCore
{
	class TestServiceClient : public AhsokaServiceFramework::AhsokaClientEndPoint<TestMessageTypes>
	{
		public:

			TestServiceClient(ServiceConfiguration config):
				AhsokaServiceFramework::AhsokaClientEndPoint<TestMessageTypes>(config, TestServiceMessages()) {}

			unique_ptr<AhsokaCore::ServiceConfiguration> EchoConfig(AhsokaCore::ServiceConfiguration& configValue, int timeoutInMs)
			{
				return SendMessageWithResponse<AhsokaCore::ServiceConfiguration>(EchoTest, &configValue, timeoutInMs);
			}
			
	};
}