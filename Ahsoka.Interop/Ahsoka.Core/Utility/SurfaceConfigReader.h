#ifndef WESTONREADER_H_
#define WESTONREADER_H_

#include "Configuration.pb.h"

static AhsokaSystem::SurfaceConfiguration config;

extern "C"
{
    void LoadSurfaceConfiguration();
    AhsokaSystem::SurfaceLayout GetSurfaceLayout(int surface_id);
}
#endif