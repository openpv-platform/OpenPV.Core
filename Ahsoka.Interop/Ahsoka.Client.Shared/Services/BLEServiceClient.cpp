#include "BLEServiceClient.h"
#include "BLEServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaBLE;

namespace AhsokaBLE
{
    typedef AhsokaClientEndPoint<BLEMessageTypes_Ids> BLEService_t;
	
	BLEServiceClient::BLEServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<BLEMessageTypes_Ids>(new BLEService_t(AhsokaConfiguration::GetServiceConfiguration(BLEServiceName), BLEServiceMessages()))
    {
        
    }

    BLEServiceClient::BLEServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<BLEMessageTypes_Ids>(new BLEService_t(config, BLEServiceMessages()))
    {
        
    }

	void BLEServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<BLEService_t>()->NotifyMessageReceived(callback);     
    }

	void BLEServiceClient::EnableAdapter(BLEAdapterRequest& bLEAdapterRequest, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_ENABLE_ADAPTER, &bLEAdapterRequest, timeoutInMs);
	}

	void BLEServiceClient::DisableAdapter(BLEAdapterRequest& bLEAdapterRequest, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_DISABLE_ADAPTER, &bLEAdapterRequest, timeoutInMs);
	}

	void BLEServiceClient::ConfirmPairingRequest(BLEPairingConfirmation& bLEPairingConfirmation, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_CONFIRM_PAIRING_REQUEST, &bLEPairingConfirmation, timeoutInMs);
	}

	void BLEServiceClient::CreateAdvertisement(BLEAdvertisementProperties& bLEAdvertisementProperties, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_CREATE_ADVERTISEMENT, &bLEAdvertisementProperties, timeoutInMs);
	}

	void BLEServiceClient::CreateService(BLEServiceProperties& bLEServiceProperties, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_CREATE_SERVICE, &bLEServiceProperties, timeoutInMs);
	}

	void BLEServiceClient::RemoveService(BLEServiceInfo& bLEServiceInfo, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_REMOVE_SERVICE, &bLEServiceInfo, timeoutInMs);
	}

	void BLEServiceClient::RemoveAdvertisement(BLEAdvertisementInfo& bLEAdvertisementInfo, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_REMOVE_ADVERTISEMENT, &bLEAdvertisementInfo, timeoutInMs);
	}

	void BLEServiceClient::SetCharacteristicValue(BLECharacteristicValue& bLECharacteristicValue, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_SET_CHARACTERISTIC_VALUE, &bLECharacteristicValue, timeoutInMs);
	}

	void BLEServiceClient::SendCharacteristicNotification(BLECharacteristicInfo& bLECharacteristicInfo, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_SEND_CHARACTERISTIC_NOTIFICATION, &bLECharacteristicInfo, timeoutInMs);
	}

	void BLEServiceClient::CharacteristicValueReadResult(BLECharacteristicValue& bLECharacteristicValue, int timeoutInMs)
	{
		GetEndPoint<BLEService_t>()->SendNotificationWithResponse(BLEMessageTypes_Ids_CHARACTERISTIC_VALUE_READ_RESULT, &bLECharacteristicValue, timeoutInMs);
	}




	#if __has_include("BLEServiceClientExtensions.cxx") 
	# include "BLEServiceClientExtensions.cxx"
	#endif
}