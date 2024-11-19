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
                RegisterServiceRequest(LayoutManagerMessageTypes::CLOSE_LOGO, CreateCTOR<AhsokaServiceFramework::EmptyNotification>(), CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
		        RegisterServiceRequest(LayoutManagerMessageTypes::UPDATE_SURFACE_LAYOUT, CreateCTOR<AhsokaLayerManager::SurfaceLayout>(),  CreateCTOR<AhsokaServiceFramework::EmptyNotification>());
			}
    };
}