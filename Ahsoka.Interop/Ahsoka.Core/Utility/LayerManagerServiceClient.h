#pragma once
#include <string>
#include <functional>
#include <memory>
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
	class LayerManagerServiceClient : public AhsokaServiceFramework::AhsokaClientEndPoint<LayoutManagerMessageTypes_Ids>
	{
		public:

			LayerManagerServiceClient():
				AhsokaServiceFramework::AhsokaClientEndPoint<LayoutManagerMessageTypes_Ids>(AhsokaConfiguration::GetServiceConfiguration(LayerManagerServiceName), 
					LayerManagerServiceMessages()) 
				{

				}

			LayerManagerServiceClient(ServiceConfiguration config):
				AhsokaServiceFramework::AhsokaClientEndPoint<LayoutManagerMessageTypes_Ids>(config, LayerManagerServiceMessages()) {}

			void CloseLogo()
			{
				SendMessageWithResponse(LayoutManagerMessageTypes_Ids::LayoutManagerMessageTypes_Ids_CLOSE_LOGO, 0, 5000);
			}

			void UpdateSurfaceLayout(SurfaceLayout & layout)
			{
				SendMessageWithResponse(LayoutManagerMessageTypes_Ids::LayoutManagerMessageTypes_Ids_UPDATE_SURFACE_LAYOUT, &layout, 5000);
			}

			static LayerManagerServiceClient& GetDefaultClient()
			{
				static auto config = ConfigReader::GetServiceConfiguration();
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