#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaSystem;

#if __has_include("SystemServiceClientIncludes.h") 
# include "SystemServiceClientIncludes.h"
#endif

namespace AhsokaSystem
{
	const std::string SystemServiceName = "SystemService";

	class SystemServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<SystemMessageTypes_Ids>
	{
		public:

			SystemServiceClient();
			SystemServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: SystemMessageTypes_Ids_IGNITION_OFF_NOTIFICATION MessageType: void
			// TransportID: SystemMessageTypes_Ids_IGNITION_ON_NOTIFICATION MessageType: void

			unique_ptr<EchoValue> Echo(EchoValue& echoValue, int timeoutInMs = INT_MAX);
			unique_ptr<ServiceInfo> GetServiceInfo(int timeoutInMs = INT_MAX);
			unique_ptr<GetHardwareInformationReply> GetHardwareInformation(int timeoutInMs = INT_MAX);
			unique_ptr<BrightnessInfo> GetBrightness(int timeoutInMs = INT_MAX);
			unique_ptr<BrightnessInfo> SetBrightness(BrightnessInfo& brightnessInfo, int timeoutInMs = INT_MAX);
			unique_ptr<BuzzerConfig> GetBuzzerConfig(int timeoutInMs = INT_MAX);
			void SetBuzzerConfig(BuzzerConfig& buzzerConfig, int timeoutInMs = INT_MAX);
			unique_ptr<UsbInfo> GetUsbInfo(int timeoutInMs = INT_MAX);
			void SetUsbInfo(UsbInfo& usbInfo, int timeoutInMs = INT_MAX);
			void SetSerialConfig(RS485ConfigNotification& rS485ConfigNotification, int timeoutInMs = INT_MAX);
			unique_ptr<VoltageValue> GetBatteryVoltage(int timeoutInMs = INT_MAX);
			unique_ptr<IgnitionState> GetIgnitionPin(int timeoutInMs = INT_MAX);
			void SetBootLogo(BootLogoInfo& bootLogoInfo, int timeoutInMs = INT_MAX);
			unique_ptr<TouchTuneInfoReply> UpdateTouchTune(TouchTuneInfo& touchTuneInfo, int timeoutInMs = INT_MAX);
			void RequestShutdown(ShutdownInformation& shutdownInformation, int timeoutInMs = INT_MAX);


			#if __has_include("SystemServiceClientExtensions.h") 
			# include "SystemServiceClientExtensions.h"
			#endif
	};
}