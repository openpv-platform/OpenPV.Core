#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaSystem
{
    class SystemServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            SystemServiceMessages()
            {
				RegisterServiceRequest(SystemMessageTypes_Ids_ECHO, CreateCTOR<AhsokaSystem::EchoValue>(), CreateCTOR<AhsokaSystem::EchoValue>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_SERVICE_INFO, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::ServiceInfo>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_HARDWARE_INFORMATION, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::GetHardwareInformationReply>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_BRIGHTNESS, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::BrightnessInfo>());
				RegisterServiceRequest(SystemMessageTypes_Ids_SET_BRIGHTNESS, CreateCTOR<AhsokaSystem::BrightnessInfo>(), CreateCTOR<AhsokaSystem::BrightnessInfo>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_BUZZER_CONFIG, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::BuzzerConfig>());
				RegisterServiceRequest(SystemMessageTypes_Ids_SET_BUZZER_CONFIG, CreateCTOR<AhsokaSystem::BuzzerConfig>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_USB_INFO, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::UsbInfo>());
				RegisterServiceRequest(SystemMessageTypes_Ids_SET_USB_INFO, CreateCTOR<AhsokaSystem::UsbInfo>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(SystemMessageTypes_Ids_SET_SERIAL_CONFIG, CreateCTOR<AhsokaSystem::RS485ConfigNotification>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_BATTERY_VOLTAGE, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::VoltageValue>());
				RegisterServiceRequest(SystemMessageTypes_Ids_GET_IGNITION_PIN, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaSystem::IgnitionState>());
				RegisterServiceRequest(SystemMessageTypes_Ids_SET_BOOT_LOGO, CreateCTOR<AhsokaSystem::BootLogoInfo>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(SystemMessageTypes_Ids_UPDATE_TOUCH_TUNE, CreateCTOR<AhsokaSystem::TouchTuneInfo>(), CreateCTOR<AhsokaSystem::TouchTuneInfoReply>());
				RegisterServiceRequest(SystemMessageTypes_Ids_REQUEST_SHUTDOWN, CreateCTOR<AhsokaSystem::ShutdownInformation>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(SystemMessageTypes_Ids_IGNITION_OFF_NOTIFICATION, CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(SystemMessageTypes_Ids_IGNITION_ON_NOTIFICATION, CreateCTOR<AhsokaServiceFramework::EmptyNotification>());

            }
    };
}