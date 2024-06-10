#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaBLE
{
    class BLEServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            BLEServiceMessages()
            {
				RegisterServiceRequest(BLEMessageTypes_Ids_ENABLE_ADAPTER, CreateCTOR<AhsokaBLE::BLEAdapterRequest>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_DISABLE_ADAPTER, CreateCTOR<AhsokaBLE::BLEAdapterRequest>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(BLEMessageTypes_Ids_ADAPTER_ENABLED, CreateCTOR<AhsokaBLE::BLEConnectionNotification>());
				RegisterServiceNotification(BLEMessageTypes_Ids_ADAPTER_DISABLED, CreateCTOR<AhsokaBLE::BLEConnectionNotification>());
				RegisterServiceNotification(BLEMessageTypes_Ids_PAIRING_REQUESTED, CreateCTOR<AhsokaBLE::BLEPairingRequest>());
				RegisterServiceNotification(BLEMessageTypes_Ids_PAIRING_CANCELLED, CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_CONFIRM_PAIRING_REQUEST, CreateCTOR<AhsokaBLE::BLEPairingConfirmation>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_CREATE_ADVERTISEMENT, CreateCTOR<AhsokaBLE::BLEAdvertisementProperties>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_CREATE_SERVICE, CreateCTOR<AhsokaBLE::BLEServiceProperties>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_REMOVE_SERVICE, CreateCTOR<AhsokaBLE::BLEServiceInfo>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_REMOVE_ADVERTISEMENT, CreateCTOR<AhsokaBLE::BLEAdvertisementInfo>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_SET_CHARACTERISTIC_VALUE, CreateCTOR<AhsokaBLE::BLECharacteristicValue>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(BLEMessageTypes_Ids_SEND_CHARACTERISTIC_NOTIFICATION, CreateCTOR<AhsokaBLE::BLECharacteristicInfo>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(BLEMessageTypes_Ids_CHARACTERISTIC_VALUE_WRITE, CreateCTOR<AhsokaBLE::BLECharacteristicValue>());
				RegisterServiceNotification(BLEMessageTypes_Ids_CHARACTERISTIC_VALUE_READ, CreateCTOR<AhsokaBLE::BLECharacteristicValue>());
				RegisterServiceRequest(BLEMessageTypes_Ids_CHARACTERISTIC_VALUE_READ_RESULT, CreateCTOR<AhsokaBLE::BLECharacteristicValue>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());

            }
    };
}