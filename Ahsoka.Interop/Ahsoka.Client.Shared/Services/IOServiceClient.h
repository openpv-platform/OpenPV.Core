#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaIO;

#if __has_include("IOServiceClientIncludes.h") 
# include "IOServiceClientIncludes.h"
#endif

namespace AhsokaIO
{
	const std::string IOServiceName = "IOService";

	class IOServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<IOMessageTypes_Ids>
	{
		public:

			IOServiceClient();
			IOServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback

			unique_ptr<SetOutputResponse> SetDigitalOutput(DigitalOutput& digitalOutput, int timeoutInMs = INT_MAX);
			unique_ptr<SetOutputResponse> SetAnalogOutput(AnalogOutput& analogOutput, int timeoutInMs = INT_MAX);
			unique_ptr<GetInputResponse> GetAnalogInput(AnalogInput& analogInput, int timeoutInMs = INT_MAX);
			unique_ptr<GetInputResponse> GetDigitalInput(DigitalInput& digitalInput, int timeoutInMs = INT_MAX);
			unique_ptr<DigitalInputList> RetrieveDigitalInputs(int timeoutInMs = INT_MAX);
			unique_ptr<AnalogInputList> RetrieveAnalogInputs(int timeoutInMs = INT_MAX);
			unique_ptr<DigitalOutputList> RetrieveDigitalOutputs(int timeoutInMs = INT_MAX);
			unique_ptr<AnalogOutputList> RetrieveAnalogOutputs(int timeoutInMs = INT_MAX);
			void SetPollInterval(PollingInterval& pollingInterval, int timeoutInMs = INT_MAX);
			unique_ptr<PollingInterval> GetPollInterval(int timeoutInMs = INT_MAX);


			#if __has_include("IOServiceClientExtensions.h") 
			# include "IOServiceClientExtensions.h"
			#endif
	};
}