#include "SystemServiceClient.h"
#include "SystemServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaSystem;

namespace AhsokaSystem
{
    typedef AhsokaClientEndPoint<SystemMessageTypes_Ids> SystemService_t;
	
	SystemServiceClient::SystemServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<SystemMessageTypes_Ids>(new SystemService_t(AhsokaConfiguration::GetServiceConfiguration(SystemServiceName), SystemServiceMessages()))
    {
        
    }

    SystemServiceClient::SystemServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<SystemMessageTypes_Ids>(new SystemService_t(config, SystemServiceMessages()))
    {
        
    }

	void SystemServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<SystemService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<EchoValue> SystemServiceClient::Echo(EchoValue& echoValue, int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<EchoValue>(SystemMessageTypes_Ids_ECHO, &echoValue, timeoutInMs);
	}

	unique_ptr<ServiceInfo> SystemServiceClient::GetServiceInfo(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<ServiceInfo>(SystemMessageTypes_Ids_GET_SERVICE_INFO, 0, timeoutInMs);
	}

	unique_ptr<GetHardwareInformationReply> SystemServiceClient::GetHardwareInformation(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<GetHardwareInformationReply>(SystemMessageTypes_Ids_GET_HARDWARE_INFORMATION, 0, timeoutInMs);
	}

	unique_ptr<BrightnessInfo> SystemServiceClient::GetBrightness(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<BrightnessInfo>(SystemMessageTypes_Ids_GET_BRIGHTNESS, 0, timeoutInMs);
	}

	unique_ptr<BrightnessInfo> SystemServiceClient::SetBrightness(BrightnessInfo& brightnessInfo, int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<BrightnessInfo>(SystemMessageTypes_Ids_SET_BRIGHTNESS, &brightnessInfo, timeoutInMs);
	}

	unique_ptr<BuzzerConfig> SystemServiceClient::GetBuzzerConfig(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<BuzzerConfig>(SystemMessageTypes_Ids_GET_BUZZER_CONFIG, 0, timeoutInMs);
	}

	void SystemServiceClient::SetBuzzerConfig(BuzzerConfig& buzzerConfig, int timeoutInMs)
	{
		GetEndPoint<SystemService_t>()->SendNotificationWithResponse(SystemMessageTypes_Ids_SET_BUZZER_CONFIG, &buzzerConfig, timeoutInMs);
	}

	unique_ptr<UsbInfo> SystemServiceClient::GetUsbInfo(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<UsbInfo>(SystemMessageTypes_Ids_GET_USB_INFO, 0, timeoutInMs);
	}

	void SystemServiceClient::SetUsbInfo(UsbInfo& usbInfo, int timeoutInMs)
	{
		GetEndPoint<SystemService_t>()->SendNotificationWithResponse(SystemMessageTypes_Ids_SET_USB_INFO, &usbInfo, timeoutInMs);
	}

	void SystemServiceClient::SetSerialConfig(RS485ConfigNotification& rS485ConfigNotification, int timeoutInMs)
	{
		GetEndPoint<SystemService_t>()->SendNotificationWithResponse(SystemMessageTypes_Ids_SET_SERIAL_CONFIG, &rS485ConfigNotification, timeoutInMs);
	}

	unique_ptr<VoltageValue> SystemServiceClient::GetBatteryVoltage(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<VoltageValue>(SystemMessageTypes_Ids_GET_BATTERY_VOLTAGE, 0, timeoutInMs);
	}

	unique_ptr<IgnitionState> SystemServiceClient::GetIgnitionPin(int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<IgnitionState>(SystemMessageTypes_Ids_GET_IGNITION_PIN, 0, timeoutInMs);
	}

	void SystemServiceClient::SetBootLogo(BootLogoInfo& bootLogoInfo, int timeoutInMs)
	{
		GetEndPoint<SystemService_t>()->SendNotificationWithResponse(SystemMessageTypes_Ids_SET_BOOT_LOGO, &bootLogoInfo, timeoutInMs);
	}

	unique_ptr<TouchTuneInfoReply> SystemServiceClient::UpdateTouchTune(TouchTuneInfo& touchTuneInfo, int timeoutInMs)
	{
		return GetEndPoint<SystemService_t>()->SendNotificationWithResponse<TouchTuneInfoReply>(SystemMessageTypes_Ids_UPDATE_TOUCH_TUNE, &touchTuneInfo, timeoutInMs);
	}

	void SystemServiceClient::RequestShutdown(ShutdownInformation& shutdownInformation, int timeoutInMs)
	{
		GetEndPoint<SystemService_t>()->SendNotificationWithResponse(SystemMessageTypes_Ids_REQUEST_SHUTDOWN, &shutdownInformation, timeoutInMs);
	}




	#if __has_include("SystemServiceClientExtensions.cxx") 
	# include "SystemServiceClientExtensions.cxx"
	#endif
}