#ifndef WESTONREADER_H_
#define WESTONREADER_H_

#include "LayerManagerService.pb.h"
#include "Configuration.pb.h"

namespace AhsokaLayerManager
{
    class ConfigReader
    {
        public: 
           
            static LayerManagerConfiguration& LoadLayerManagerConfig();
            static SurfaceLayout GetSurfaceLayout(int surface_id);
            static void UpdateSurfaceLayout(SurfaceLayout& newLayout);
            static AhsokaSystem::ServiceConfiguration GetServiceConfiguration();

        private:
            static AhsokaLayerManager::LayerManagerConfiguration config;

    };
}
#endif