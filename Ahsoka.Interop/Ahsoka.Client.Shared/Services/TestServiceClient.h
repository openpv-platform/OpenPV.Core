#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaConfiguration.h"
#include "AhsokaClientEndPoint.h"
#include "Services.pb.h"
#include "SystemService.pb.h"
#include "TestServiceMessages.hpp"

using namespace std;
using namespace AhsokaSystem;
using namespace AhsokaServiceFramework;

namespace AhsokaSystem
{

    class TestEchoValue {};
    enum TestMessageTypes : int
    {
        Test = 1
    };

	const std::string TestServiceName = "TestService";
	typedef AhsokaClientEndPoint<TestMessageTypes> TestService_t;

	class TestServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<TestMessageTypes>
	{
		public:

			TestServiceClient(): 
				AhsokaServiceFramework::AhsokaEndPointWithCallback<TestMessageTypes>(new TestService_t(AhsokaConfiguration::GetServiceConfiguration(TestServiceName), TestServiceMessages())) {}

			TestServiceClient(ServiceConfiguration config):
				AhsokaServiceFramework::AhsokaEndPointWithCallback<TestMessageTypes>(new TestService_t(config, TestServiceMessages())) {}

			void RegisterListener(MessageReceived_t callback) {}


			unique_ptr<EchoValue> Echo(EchoValue& echoValue, int timeoutInMs)
            {
                return GetEndPoint<TestService_t>()->SendNotificationWithResponse<EchoValue>(Test, &echoValue, timeoutInMs);			
            }
	};
}