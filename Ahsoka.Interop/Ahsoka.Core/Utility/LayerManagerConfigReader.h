#ifndef WESTONREADER_H_
#define WESTONREADER_H_

#include "LayerManagerService.pb.h"

static AhsokaLayerManager::LayerManagerConfiguration config;

extern "C"
{
    void LoadLayerManagerConfig();
    AhsokaLayerManager::SurfaceLayout GetSurfaceLayout(int surface_id);
}
#endif