#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaIO
{
    class IOServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            IOServiceMessages()
            {
				RegisterServiceRequest(IOMessageTypes_Ids_SET_DIGITAL_OUTPUT, CreateCTOR<AhsokaIO::DigitalOutput>(), CreateCTOR<AhsokaIO::SetOutputResponse>());
				RegisterServiceRequest(IOMessageTypes_Ids_SET_ANALOG_OUTPUT, CreateCTOR<AhsokaIO::AnalogOutput>(), CreateCTOR<AhsokaIO::SetOutputResponse>());
				RegisterServiceRequest(IOMessageTypes_Ids_GET_ANALOG_INPUT, CreateCTOR<AhsokaIO::AnalogInput>(), CreateCTOR<AhsokaIO::GetInputResponse>());
				RegisterServiceRequest(IOMessageTypes_Ids_GET_DIGITAL_INPUT, CreateCTOR<AhsokaIO::DigitalInput>(), CreateCTOR<AhsokaIO::GetInputResponse>());
				RegisterServiceRequest(IOMessageTypes_Ids_RETRIEVE_DIGITAL_INPUTS, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaIO::DigitalInputList>());
				RegisterServiceRequest(IOMessageTypes_Ids_RETRIEVE_ANALOG_INPUTS, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaIO::AnalogInputList>());
				RegisterServiceRequest(IOMessageTypes_Ids_RETRIEVE_DIGITAL_OUTPUTS, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaIO::DigitalOutputList>());
				RegisterServiceRequest(IOMessageTypes_Ids_RETRIEVE_ANALOG_OUTPUTS, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaIO::AnalogOutputList>());
				RegisterServiceRequest(IOMessageTypes_Ids_SET_POLL_INTERVAL, CreateCTOR<AhsokaIO::PollingInterval>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(IOMessageTypes_Ids_GET_POLL_INTERVAL, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaIO::PollingInterval>());

            }
    };
}