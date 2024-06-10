#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaNetwork
{
    class NetworkServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            NetworkServiceMessages()
            {
				RegisterServiceRequest(NetworkMessageTypes_Ids_GET_ADAPTERS, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaNetwork::AvailableAdapters>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_ENABLE_ADAPTER, CreateCTOR<AhsokaNetwork::AdapterRequest>(), CreateCTOR<AhsokaNetwork::AdapterInfo>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_DISABLE_ADAPTER, CreateCTOR<AhsokaNetwork::AdapterRequest>(), CreateCTOR<AhsokaNetwork::AdapterInfo>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_GET_ADAPTER_CONFIGURATION, CreateCTOR<AhsokaNetwork::AdapterRequest>(), CreateCTOR<AhsokaNetwork::AdapterConfiguration>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_SET_ADAPTER_CONFIGURATION, CreateCTOR<AhsokaNetwork::AdapterConfiguration>(), CreateCTOR<AhsokaNetwork::AdapterConfigurationResult>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_DISCOVER_NETWORKS, CreateCTOR<AhsokaNetwork::AdapterRequest>(), CreateCTOR<AhsokaNetwork::WirelessNetworkInfoList>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_CONNECT_TO_NETWORK, CreateCTOR<AhsokaNetwork::WirelessConnectionRequest>(), CreateCTOR<AhsokaNetwork::WirelessConnectionResponse>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_DISCONNECT_NETWORK, CreateCTOR<AhsokaNetwork::AdapterRequest>(), CreateCTOR<AhsokaNetwork::WirelessConnectionNotification>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_GET_NETWORK_PROFILES, CreateCTOR<AhsokaNetwork::ProfileRequest>(), CreateCTOR<AhsokaNetwork::NetworkProfilesList>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_SET_NETWORK_PROFILES, CreateCTOR<AhsokaNetwork::NetworkProfilesList>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_GET_NETWORK_INFO, CreateCTOR<AhsokaNetwork::AdapterRequest>(), CreateCTOR<AhsokaNetwork::IPInfo>());
				RegisterServiceRequest(NetworkMessageTypes_Ids_ATTEMPT_AUTO_CONNECT, CreateCTOR<AhsokaNetwork::AutoConnectRequest>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
				RegisterServiceNotification(NetworkMessageTypes_Ids_CONNECTION_STATE_NOTIFICATION, CreateCTOR<AhsokaNetwork::WirelessConnectionNotification>());
				RegisterServiceNotification(NetworkMessageTypes_Ids_NETWORK_ADAPTER_READY, CreateCTOR<AhsokaNetwork::AdapterInfo>());
				RegisterServiceNotification(NetworkMessageTypes_Ids_WIRELESS_SIGNAL_CHANGED, CreateCTOR<AhsokaNetwork::WirelessNetworkInfo>());

            }
    };
}