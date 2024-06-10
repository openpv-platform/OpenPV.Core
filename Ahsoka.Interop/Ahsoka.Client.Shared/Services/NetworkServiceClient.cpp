#include "NetworkServiceClient.h"
#include "NetworkServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaNetwork;

namespace AhsokaNetwork
{
    typedef AhsokaClientEndPoint<NetworkMessageTypes_Ids> NetworkService_t;
	
	NetworkServiceClient::NetworkServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<NetworkMessageTypes_Ids>(new NetworkService_t(AhsokaConfiguration::GetServiceConfiguration(NetworkServiceName), NetworkServiceMessages()))
    {
        
    }

    NetworkServiceClient::NetworkServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<NetworkMessageTypes_Ids>(new NetworkService_t(config, NetworkServiceMessages()))
    {
        
    }

	void NetworkServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<NetworkService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<AvailableAdapters> NetworkServiceClient::GetAdapters(int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<AvailableAdapters>(NetworkMessageTypes_Ids_GET_ADAPTERS, 0, timeoutInMs);
	}

	unique_ptr<AdapterInfo> NetworkServiceClient::EnableAdapter(AdapterRequest& adapterRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<AdapterInfo>(NetworkMessageTypes_Ids_ENABLE_ADAPTER, &adapterRequest, timeoutInMs);
	}

	unique_ptr<AdapterInfo> NetworkServiceClient::DisableAdapter(AdapterRequest& adapterRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<AdapterInfo>(NetworkMessageTypes_Ids_DISABLE_ADAPTER, &adapterRequest, timeoutInMs);
	}

	unique_ptr<AdapterConfiguration> NetworkServiceClient::GetAdapterConfiguration(AdapterRequest& adapterRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<AdapterConfiguration>(NetworkMessageTypes_Ids_GET_ADAPTER_CONFIGURATION, &adapterRequest, timeoutInMs);
	}

	unique_ptr<AdapterConfigurationResult> NetworkServiceClient::SetAdapterConfiguration(AdapterConfiguration& adapterConfiguration, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<AdapterConfigurationResult>(NetworkMessageTypes_Ids_SET_ADAPTER_CONFIGURATION, &adapterConfiguration, timeoutInMs);
	}

	unique_ptr<WirelessNetworkInfoList> NetworkServiceClient::DiscoverNetworks(AdapterRequest& adapterRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<WirelessNetworkInfoList>(NetworkMessageTypes_Ids_DISCOVER_NETWORKS, &adapterRequest, timeoutInMs);
	}

	unique_ptr<WirelessConnectionResponse> NetworkServiceClient::ConnectToNetwork(WirelessConnectionRequest& wirelessConnectionRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<WirelessConnectionResponse>(NetworkMessageTypes_Ids_CONNECT_TO_NETWORK, &wirelessConnectionRequest, timeoutInMs);
	}

	unique_ptr<WirelessConnectionNotification> NetworkServiceClient::DisconnectNetwork(AdapterRequest& adapterRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<WirelessConnectionNotification>(NetworkMessageTypes_Ids_DISCONNECT_NETWORK, &adapterRequest, timeoutInMs);
	}

	unique_ptr<NetworkProfilesList> NetworkServiceClient::GetNetworkProfiles(ProfileRequest& profileRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<NetworkProfilesList>(NetworkMessageTypes_Ids_GET_NETWORK_PROFILES, &profileRequest, timeoutInMs);
	}

	void NetworkServiceClient::SetNetworkProfiles(NetworkProfilesList& networkProfilesList, int timeoutInMs)
	{
		GetEndPoint<NetworkService_t>()->SendNotificationWithResponse(NetworkMessageTypes_Ids_SET_NETWORK_PROFILES, &networkProfilesList, timeoutInMs);
	}

	unique_ptr<IPInfo> NetworkServiceClient::GetNetworkInfo(AdapterRequest& adapterRequest, int timeoutInMs)
	{
		return GetEndPoint<NetworkService_t>()->SendNotificationWithResponse<IPInfo>(NetworkMessageTypes_Ids_GET_NETWORK_INFO, &adapterRequest, timeoutInMs);
	}

	void NetworkServiceClient::AttemptAutoConnect(AutoConnectRequest& autoConnectRequest, int timeoutInMs)
	{
		GetEndPoint<NetworkService_t>()->SendNotificationWithResponse(NetworkMessageTypes_Ids_ATTEMPT_AUTO_CONNECT, &autoConnectRequest, timeoutInMs);
	}




	#if __has_include("NetworkServiceClientExtensions.cxx") 
	# include "NetworkServiceClientExtensions.cxx"
	#endif
}