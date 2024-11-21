#pragma once
#include <functional>
#include <memory>
#include <string>
#include <fstream>
#include <sstream>
#include <filesystem>
#include "AhsokaConfiguration.h"
#include "AhsokaRuntimeBuilder.h"
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

			void UpdateSurfaceLayout(SurfaceSetup & setup)
			{
				SendMessageWithResponse(LayerManagerMessageTypes_Ids::LayerManagerMessageTypes_Ids_UPDATE_SURFACE_SETUP, &setup, 5000);
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
				printf("LayerManager Config Reader: Loading Configuration\n");

				std::string file = "/usr/local/Ahsoka/current/.appinfo/SurfaceConfiguration.pbuff";
				
				if (filesystem::exists(file))
				{
					std::ifstream ifs(file, std::ifstream::in);
					config.ParseFromIstream(&ifs);

					for (SurfaceSetup setup : config.surface_setup())
						printf("LayerManager Config Reader: Found SurfaceID %d\n", setup.surface_id() );
				}
				else
				{
					config.set_show_unknown_surfaces(true);
					printf ("LayerManager Config Reader: No SurfaceConfiguration Found...\n");
				}
			}

			static AhsokaSystem::ServiceConfiguration GetServiceConfiguration()
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

			static LayerManagerServiceClient& GetDefaultClient()
			{
				static auto config = LayerManagerServiceClient::GetServiceConfiguration();
				static LayerManagerServiceClient client(config);

				if (!client.GetIsConnected())
				{
					std::cout << "Starting Service Client on " << config.data_channel() << std::endl;
					client.Start();
				}

				return client;
			}
	};
}