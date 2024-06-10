#include "IOServiceClient.h"
#include "IOServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaIO;

namespace AhsokaIO
{
    typedef AhsokaClientEndPoint<IOMessageTypes_Ids> IOService_t;
	
	IOServiceClient::IOServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<IOMessageTypes_Ids>(new IOService_t(AhsokaConfiguration::GetServiceConfiguration(IOServiceName), IOServiceMessages()))
    {
        
    }

    IOServiceClient::IOServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<IOMessageTypes_Ids>(new IOService_t(config, IOServiceMessages()))
    {
        
    }

	void IOServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<IOService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<SetOutputResponse> IOServiceClient::SetDigitalOutput(DigitalOutput& digitalOutput, int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<SetOutputResponse>(IOMessageTypes_Ids_SET_DIGITAL_OUTPUT, &digitalOutput, timeoutInMs);
	}

	unique_ptr<SetOutputResponse> IOServiceClient::SetAnalogOutput(AnalogOutput& analogOutput, int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<SetOutputResponse>(IOMessageTypes_Ids_SET_ANALOG_OUTPUT, &analogOutput, timeoutInMs);
	}

	unique_ptr<GetInputResponse> IOServiceClient::GetAnalogInput(AnalogInput& analogInput, int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<GetInputResponse>(IOMessageTypes_Ids_GET_ANALOG_INPUT, &analogInput, timeoutInMs);
	}

	unique_ptr<GetInputResponse> IOServiceClient::GetDigitalInput(DigitalInput& digitalInput, int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<GetInputResponse>(IOMessageTypes_Ids_GET_DIGITAL_INPUT, &digitalInput, timeoutInMs);
	}

	unique_ptr<DigitalInputList> IOServiceClient::RetrieveDigitalInputs(int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<DigitalInputList>(IOMessageTypes_Ids_RETRIEVE_DIGITAL_INPUTS, 0, timeoutInMs);
	}

	unique_ptr<AnalogInputList> IOServiceClient::RetrieveAnalogInputs(int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<AnalogInputList>(IOMessageTypes_Ids_RETRIEVE_ANALOG_INPUTS, 0, timeoutInMs);
	}

	unique_ptr<DigitalOutputList> IOServiceClient::RetrieveDigitalOutputs(int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<DigitalOutputList>(IOMessageTypes_Ids_RETRIEVE_DIGITAL_OUTPUTS, 0, timeoutInMs);
	}

	unique_ptr<AnalogOutputList> IOServiceClient::RetrieveAnalogOutputs(int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<AnalogOutputList>(IOMessageTypes_Ids_RETRIEVE_ANALOG_OUTPUTS, 0, timeoutInMs);
	}

	void IOServiceClient::SetPollInterval(PollingInterval& pollingInterval, int timeoutInMs)
	{
		GetEndPoint<IOService_t>()->SendNotificationWithResponse(IOMessageTypes_Ids_SET_POLL_INTERVAL, &pollingInterval, timeoutInMs);
	}

	unique_ptr<PollingInterval> IOServiceClient::GetPollInterval(int timeoutInMs)
	{
		return GetEndPoint<IOService_t>()->SendNotificationWithResponse<PollingInterval>(IOMessageTypes_Ids_GET_POLL_INTERVAL, 0, timeoutInMs);
	}




	#if __has_include("IOServiceClientExtensions.cxx") 
	# include "IOServiceClientExtensions.cxx"
	#endif
}