#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaNetwork;

#if __has_include("NetworkServiceClientIncludes.h") 
# include "NetworkServiceClientIncludes.h"
#endif

namespace AhsokaNetwork
{
	const std::string NetworkServiceName = "NetworkService";

	class NetworkServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<NetworkMessageTypes_Ids>
	{
		public:

			NetworkServiceClient();
			NetworkServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback
			// TransportID: NetworkMessageTypes_Ids_CONNECTION_STATE_NOTIFICATION MessageType: unique_ptr<WirelessConnectionNotification>
			// TransportID: NetworkMessageTypes_Ids_NETWORK_ADAPTER_READY MessageType: unique_ptr<AdapterInfo>
			// TransportID: NetworkMessageTypes_Ids_WIRELESS_SIGNAL_CHANGED MessageType: unique_ptr<WirelessNetworkInfo>

			unique_ptr<AvailableAdapters> GetAdapters(int timeoutInMs = INT_MAX);
			unique_ptr<AdapterInfo> EnableAdapter(AdapterRequest& adapterRequest, int timeoutInMs = INT_MAX);
			unique_ptr<AdapterInfo> DisableAdapter(AdapterRequest& adapterRequest, int timeoutInMs = INT_MAX);
			unique_ptr<AdapterConfiguration> GetAdapterConfiguration(AdapterRequest& adapterRequest, int timeoutInMs = INT_MAX);
			unique_ptr<AdapterConfigurationResult> SetAdapterConfiguration(AdapterConfiguration& adapterConfiguration, int timeoutInMs = INT_MAX);
			unique_ptr<WirelessNetworkInfoList> DiscoverNetworks(AdapterRequest& adapterRequest, int timeoutInMs = INT_MAX);
			unique_ptr<WirelessConnectionResponse> ConnectToNetwork(WirelessConnectionRequest& wirelessConnectionRequest, int timeoutInMs = INT_MAX);
			unique_ptr<WirelessConnectionNotification> DisconnectNetwork(AdapterRequest& adapterRequest, int timeoutInMs = INT_MAX);
			unique_ptr<NetworkProfilesList> GetNetworkProfiles(ProfileRequest& profileRequest, int timeoutInMs = INT_MAX);
			void SetNetworkProfiles(NetworkProfilesList& networkProfilesList, int timeoutInMs = INT_MAX);
			unique_ptr<IPInfo> GetNetworkInfo(AdapterRequest& adapterRequest, int timeoutInMs = INT_MAX);
			void AttemptAutoConnect(AutoConnectRequest& autoConnectRequest, int timeoutInMs = INT_MAX);


			#if __has_include("NetworkServiceClientExtensions.h") 
			# include "NetworkServiceClientExtensions.h"
			#endif
	};
}