#pragma once

#include <iostream>
#include <string>
#include <chrono>
#include <thread>

#include "AhsokaServices.h"

class NetworkTests
{
    public:
        void RunTests(std::string pathToServiceExe)
        {
            SystemServiceClient client;
            NetworkServiceClient networkClient;

            auto runtime = AhsokaRuntime::CreateBuilder()
                .AddEndPoint(&client)
                .AddEndPoint(&networkClient)
                .SetServiceExecutable(pathToServiceExe)
                .StartWithExternalServices(); // Connect to Already Running Runtime

            std::cout << client.Echo("Server Ready") << ::endl;

            string wifiAdapterId;

            auto networkAdapters = networkClient.GetAdapters();
            for (auto adapter: networkAdapters.get()->adapters())
            {
                std::cout << std::endl << "Adapter: " << adapter.description() << " is of type " << adapter.adapter_type() << " with AdapterId " << adapter.adapter_id();
                std::this_thread::sleep_for(std::chrono::milliseconds(1000));
                AdapterRequest request;
                request.set_adapter_id(adapter.adapter_id());
                auto config = networkClient.GetAdapterConfiguration(request);

                if (adapter.adapter_id().compare("2")) //Wifi client
                {
                    wifiAdapterId = adapter.adapter_id();
                    config.get()->mutable_client_configuration()->set_connection_type(IPAddressConfiguration::DHCP);
                }

                networkClient.SetAdapterConfiguration(*config);

                auto updatedConfig = networkClient.GetAdapterConfiguration(request);
                if (!(config.get()->adapter_id() == updatedConfig.get()->adapter_id() 
                    && config.get()->client_configuration().connection_type() == updatedConfig.get()->client_configuration().connection_type()))
                {
                    std::cout << std::endl << "Configs did not match: failure";
                }
            }

            if (wifiAdapterId.compare(""))
            {
                AdapterRequest wifiRequest;
                wifiRequest.set_adapter_id(wifiAdapterId);
                auto networkList = networkClient.DiscoverNetworks(wifiRequest);

                std::cout << std::endl << "Discovered Networks";
                for (WirelessNetworkInfo item: networkList.get()->networks())
                {
                    std::cout << std::endl << "SSID:" << item.ssid() << " Signal:" << item.signal_level() << " Security Type:" << item.security_type();
                }

                std::cout << std::endl << "Network Profiles";
                ProfileRequest profileRequest;
                profileRequest.set_adapter_id(wifiAdapterId);
                profileRequest.set_use_os_profiles(false);
                auto profiles = networkClient.GetNetworkProfiles(profileRequest);
                for (WirelessConnectionRequest item: profiles.get()->network_profiles())
                {
                    std::cout << std::endl << "SSID:" << item.ssid();
                }
                std::cout << std::endl;

            }

            client.RequestServiceShutdown();
            runtime.RequestShutdown();
            runtime.WaitForShutdown();

            std::cout << "Shutdown Completed" << std::endl;
        }
};
