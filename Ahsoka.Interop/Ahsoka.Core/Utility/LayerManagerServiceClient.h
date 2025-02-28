#pragma once
#include <functional>
#include <memory>
#include <string>
#include <fstream>
#include <sstream>
#include <filesystem>
#include "AhsokaConfiguration.h"
#include "AhsokaConfiguration.h"
#include "AhsokaClientEndPoint.h"
#include "Services.pb.h"
#include "LayerManagerServiceMessages.h"

using namespace std;
using namespace AhsokaLayerManager;
using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{
	class LayerManagerServiceClient : public AhsokaServiceFramework::AhsokaClientEndPoint<LayerManagerMessageTypes_Ids>
	{
		public:

			LayerManagerServiceClient():
				AhsokaServiceFramework::AhsokaClientEndPoint<LayerManagerMessageTypes_Ids>(AhsokaConfiguration::GetServiceConfiguration(LayerManagerServiceName), 
					LayerManagerServiceMessages()) 
				{

				}

			LayerManagerServiceClient(ServiceConfiguration config):
				AhsokaServiceFramework::AhsokaClientEndPoint<LayerManagerMessageTypes_Ids>(config, LayerManagerServiceMessages()) {}

			void StopLogo()
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_STOP_LOGO, 0, 5000);
			}

			void UpdateSurfaceSetup(SurfaceSetup & setup)
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_UPDATE_SURFACE_SETUP, &setup, 5000);
			}

			std::unique_ptr<SurfaceSetup> GetSurfaceSetup(SurfaceRequest & request)
			{
				return SendMessageWithResponse<SurfaceSetup>(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_GET_SURFACE_SETUP, &request, 5000);
			}

			void UpdateSurfaceLocation(SurfaceRectangle & locationInfo)
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_UPDATE_SURFACE_LOCATION, &locationInfo, 5000);
			}

			void UpdateSurfaceSource(SurfaceRectangle & sourceInfo)
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_UPDATE_SURFACE_SOURCE, &sourceInfo, 5000);
			}

			void UpdateSurfaceVisibility(SurfaceVisibility & visibilityInfo)
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_UPDATE_SURFACE_VISIBILITY, &visibilityInfo, 5000);
			}

			void UpdateSurfaceVisibility(SurfaceOrder & order)
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_UPDATE_SURFACE_Z_ORDER, &order, 5000);
			}


			static void LoadLayerManagerConfig(AhsokaLayerManager::LayerManagerConfiguration& config)
			{
				std::string file = "/usr/local/Ahsoka/current/.appinfo/SurfaceConfiguration.pbuff";
				
				if (filesystem::exists(file))
				{
					std::ifstream ifs(file, std::ifstream::in);
					config.set_show_unknown_surfaces(true); // Temp Until we get closer to production
					config.ParseFromIstream(&ifs);	}
				else
				{
					config.set_show_unknown_surfaces(true);
					printf ("LayerManager Config Reader: No SurfaceConfiguration Found...\n");
				}
			}

			static AhsokaCore::ServiceConfiguration GetServiceConfiguration()
			{
				auto config = AhsokaConfiguration::GetServiceConfiguration(LayerManagerServiceName, false);
				if (config.data_channel() > 0)
				{
					cout << "LayerManager: Service Configuration Found - Starting Service on " << config.data_channel() << std::endl;
				}
				else
				{
					cout << "LayerManager: Service Configuration Not Found - Starting Service On Port 5100." << std::endl;
					config.set_service_name(LayerManagerServiceName);
					config.set_tcp_listen_address("*");
					config.set_tcp_connection_address("localhost");
					config.set_data_channel(5100);
				}

				return config;
			}

			static LayerManagerServiceClient* GetDefaultClient(bool startClient = true)
			{
				static LayerManagerServiceClient* client = 0;
				if (client == 0 && startClient) 
				{
					auto config = LayerManagerServiceClient::GetServiceConfiguration();
					client = new LayerManagerServiceClient(config);
					client->Start();
					std::cout << "Starting Service Client on " << config.data_channel() << std::endl;
				}
				
				return client;
			}

	};
}