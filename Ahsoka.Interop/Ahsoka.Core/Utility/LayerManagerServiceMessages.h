#pragma once

#include <string>
#include <functional>
#include "AhsokaConfiguration.h"
#include "LayerManagerService.pb.h"

using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{
    const std::string LayerManagerServiceName = "LayerManagerService";

    class LayerManagerServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            LayerManagerServiceMessages()
            {
                RegisterServiceRequest(LayerManagerMessageTypes::STOP_LOGO, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
		        RegisterServiceRequest(LayerManagerMessageTypes::UPDATE_SURFACE_SETUP, CreateCTOR<AhsokaLayerManager::SurfaceSetup>(),  CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
		        RegisterServiceRequest(LayerManagerMessageTypes::GET_SURFACE_SETUP, CreateCTOR<AhsokaLayerManager::SurfaceRequest>(),  CreateCTOR<AhsokaLayerManager::SurfaceSetup>());
		        RegisterServiceRequest(LayerManagerMessageTypes::UPDATE_SURFACE_LOCATION, CreateCTOR<AhsokaLayerManager::SurfaceRectangle>(),  CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
		        RegisterServiceRequest(LayerManagerMessageTypes::UPDATE_SURFACE_SOURCE, CreateCTOR<AhsokaLayerManager::SurfaceRectangle>(),  CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
		        RegisterServiceRequest(LayerManagerMessageTypes::UPDATE_SURFACE_VISIBILITY, CreateCTOR<AhsokaLayerManager::SurfaceVisibility>(),  CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
		        RegisterServiceRequest(LayerManagerMessageTypes::UPDATE_SURFACE_Z_ORDER, CreateCTOR<AhsokaLayerManager::SurfaceOrder>(),  CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
			}
    };
}