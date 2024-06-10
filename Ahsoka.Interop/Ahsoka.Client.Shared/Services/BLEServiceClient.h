#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaBLE;

#if __has_include("BLEServiceClientIncludes.h") 
# include "BLEServiceClientIncludes.h"
#endif

namespace AhsokaBLE
{
	const std::string BLEServiceName = "BLEService";

	class BLEServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<BLEMessageTypes_Ids>
	{
		public:

			BLEServiceClient();
			BLEServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: BLEMessageTypes_Ids_ADAPTER_ENABLED MessageType: unique_ptr<BLEConnectionNotification>
			// TransportID: BLEMessageTypes_Ids_ADAPTER_DISABLED MessageType: unique_ptr<BLEConnectionNotification>
			// TransportID: BLEMessageTypes_Ids_PAIRING_REQUESTED MessageType: unique_ptr<BLEPairingRequest>
			// TransportID: BLEMessageTypes_Ids_PAIRING_CANCELLED MessageType: void
			// TransportID: BLEMessageTypes_Ids_CHARACTERISTIC_VALUE_WRITE MessageType: unique_ptr<BLECharacteristicValue>
			// TransportID: BLEMessageTypes_Ids_CHARACTERISTIC_VALUE_READ MessageType: unique_ptr<BLECharacteristicValue>

			void EnableAdapter(BLEAdapterRequest& bLEAdapterRequest, int timeoutInMs = INT_MAX);
			void DisableAdapter(BLEAdapterRequest& bLEAdapterRequest, int timeoutInMs = INT_MAX);
			void ConfirmPairingRequest(BLEPairingConfirmation& bLEPairingConfirmation, int timeoutInMs = INT_MAX);
			void CreateAdvertisement(BLEAdvertisementProperties& bLEAdvertisementProperties, int timeoutInMs = INT_MAX);
			void CreateService(BLEServiceProperties& bLEServiceProperties, int timeoutInMs = INT_MAX);
			void RemoveService(BLEServiceInfo& bLEServiceInfo, int timeoutInMs = INT_MAX);
			void RemoveAdvertisement(BLEAdvertisementInfo& bLEAdvertisementInfo, int timeoutInMs = INT_MAX);
			void SetCharacteristicValue(BLECharacteristicValue& bLECharacteristicValue, int timeoutInMs = INT_MAX);
			void SendCharacteristicNotification(BLECharacteristicInfo& bLECharacteristicInfo, int timeoutInMs = INT_MAX);
			void CharacteristicValueReadResult(BLECharacteristicValue& bLECharacteristicValue, int timeoutInMs = INT_MAX);


			#if __has_include("BLEServiceClientExtensions.h") 
			# include "BLEServiceClientExtensions.h"
			#endif
	};
}