#include <stdio.h>
#include <stdlib.h>
#include <string>
#include <fstream>
#include <sstream>
#include <filesystem>

#include "LayerManagerConfigReader.h"
#include "AhsokaConfiguration.h"

using namespace std;
using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{

    const string serviceName = "LayerManagerService";
    AhsokaLayerManager::LayerManagerConfiguration ConfigReader::config;

    LayerManagerConfiguration& ConfigReader::LoadLayerManagerConfig()
    {
        printf("LayerManager Config Reader: Loading Configuration\n");
        
        std::string file = "/usr/local/Ahsoka/current/.appinfo/SurfaceConfiguration.pbuff";
        
        if (filesystem::exists(file))
        {
            std::ifstream ifs(file, std::ifstream::in);
            config.ParseFromIstream(&ifs);

            for (SurfaceLayout layout : config.surface_layouts())
                printf("LayerManager Config Reader: Found SurfaceID %d\n", layout.surface_id() );
        }
        else
        {
            config.set_show_unknown_surfaces(true);
            printf ("LayerManager Config Reader: No SurfaceConfiguration Found...\n");
        }

        return config;
    }


    SurfaceLayout ConfigReader::GetSurfaceLayout(int surface_id)
    {
        SurfaceLayout emptyLayout;
        for (SurfaceLayout layout : config.surface_layouts())
        if (layout.surface_id() == surface_id)
            return layout;

        return emptyLayout;
    }

    void ConfigReader::UpdateSurfaceLayout(SurfaceLayout& newLayout)
    {
        for (size_t i = 0; i < config.surface_layouts().size(); i++)
        {
            // Remove Existing Item..if exists
            if (config.surface_layouts()[i].surface_id() == newLayout.surface_id())
            {
                config.mutable_surface_layouts()->SwapElements(i, config.mutable_surface_layouts()->size() - 1);
                config.mutable_surface_layouts()->RemoveLast();
            }
        }

        // Add Updated Item to List
        config.mutable_surface_layouts()->Add()->CopyFrom(newLayout);
    }

    AhsokaSystem::ServiceConfiguration ConfigReader::GetServiceConfiguration()
    {
        auto config = AhsokaConfiguration::GetServiceConfiguration(serviceName, false);
        if (config.data_channel() > 0)
        {
             cout << "LayerManager: Service Configuration Found - Starting Service on " << config.data_channel() << std::endl;
        }
        else
        {
            cout << "LayerManager: Service Configuration Not Found - Starting Service On Port 5100." << std::endl;
            config.set_service_name(serviceName);
            config.set_tcp_listen_address("*");
            config.set_tcp_connection_address("localhost");
            config.set_data_channel(5100);
        }

        return config;
    }

}
