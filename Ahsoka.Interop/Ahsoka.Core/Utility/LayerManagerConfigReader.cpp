#include <stdio.h>
#include <stdlib.h>
#include <string>
#include <fstream>
#include <sstream>
#include <filesystem>

#include "LayerManagerConfigReader.h"

using namespace std;
using namespace AhsokaLayerManager;

void LoadLayerManagerConfig()
{
    printf("Loading Configuration\n");
    
    std::string file = "/usr/local/Ahsoka/current/.appinfo/SurfaceConfiguration.pbuff";
    
    if (filesystem::exists(file))
    {
        std::ifstream ifs(file, std::ifstream::in);
        config.ParseFromIstream(&ifs);

        for (SurfaceLayout layout : config.surface_layouts())
            printf("Found SurfaceID %d\n", layout.surface_id() );
    }
    else
    {
        printf ("No SurfaceConfiguration Found...\n");
    }
}

SurfaceLayout GetSurfaceLayout(int surface_id)
{
    SurfaceLayout emptyLayout;
    for (SurfaceLayout layout : config.surface_layouts())
       if (layout.surface_id() == surface_id)
           return layout;

    return emptyLayout;
}


