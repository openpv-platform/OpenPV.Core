#ifndef LAYERMANAGER_H_
#define LAYERMANAGER_H_

#include <pthread.h>
#include <wayland-client.h>
#include <wayland-server.h>
#include "ilm/ilm_control.h"
#include <ilm/ivi-application-client-protocol.h>

int Init();
void GetScreenInfo();
void CreateScreenLayers();
void AppRun();

void configure_ilm_surface(t_ilm_uint id, t_ilm_uint width, t_ilm_uint height);
void surfaceCallbackFunction(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m);
void callbackFunction(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data);
void shutdownCallbackFunction(t_ilm_shutdown_error_type error_type, int errornum, void *user_data);

#endif