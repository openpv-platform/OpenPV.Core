
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <pthread.h>
#include <getopt.h>

#include <wayland-client.h>
#include <wayland-server.h>
#include "ilm/ilm_control.h"
#include <ilm/ivi-application-client-protocol.h>

// We Default to Layer 1000 and Screen 0
t_ilm_uint layer = 1000;
t_ilm_uint screen_ID = 0;
t_ilm_uint screenWidth;
t_ilm_uint screenHeight;
struct ivi_application *ivi_app;
struct wl_compositor *compositor;
struct wl_display *display;
struct wl_registry *registry;

pthread_mutex_t mutex;
static pthread_cond_t  waiterVariable = PTHREAD_COND_INITIALIZER;
static int number_of_surfaces = 100;

static void configure_ilm_surface(t_ilm_uint id, t_ilm_uint width, t_ilm_uint height)
{
    ilm_surfaceSetDestinationRectangle(id, 0, 0, width, height);
    ilm_surfaceSetSourceRectangle(id, 0, 0, width, height);
    ilm_surfaceSetVisibility(id, ILM_TRUE);
    ilm_layerAddSurface(layer,id);
    ilm_surfaceRemoveNotification(id);


    ilm_commitChanges();
    pthread_cond_signal( &waiterVariable );

    printf("layer-add-surfaces: surface (%u) configured with:\n"
           "    dst region: x:0 y:0 w:%u h:%u\n"
           "    src region: x:0 y:0 w:%u h:%u\n"
           "    visibility: TRUE\n"
           "    added to layer (%u)\n", id, width, height, width, height,layer);
}

static void surfaceCallbackFunction(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m)
{
    if ((unsigned)m & ILM_NOTIFICATION_CONFIGURED)
    {
        configure_ilm_surface(id, sp->origSourceWidth, sp->origSourceHeight);
    }
}

static void callbackFunction(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data)
{
    (void)user_data;
    struct ilmSurfaceProperties sp;

    if (object == ILM_SURFACE) 
	{
        if (created) 
		{
            if (number_of_surfaces > 0) 
			{
                number_of_surfaces--;
                printf("LayerManager: Surface (%d) Created\n",id);
                
				// always get configured event to follow the surface changings
                ilm_surfaceAddNotification(id,&surfaceCallbackFunction);
                ilm_commitChanges();
                ilm_getPropertiesOfSurface(id, &sp);

                if ((sp.origSourceWidth != 0) && (sp.origSourceHeight !=0))
                {   
					// surface is already configured
                    configure_ilm_surface(id, sp.origSourceWidth, sp.origSourceHeight);
                }
            }
        }
        else if (!created)
            printf("LayerManager: Surface (%u) Destroyed\n",id);
    } 
	else if (object == ILM_LAYER) 
	{
        if (created)
            printf("LayerManager: Layer (%u) Created\n",id);
        else if(!created)
            printf("LayerManager: Layer (%u) Destroyed\n",id);
    }
}

static void shutdownCallbackFunction(t_ilm_shutdown_error_type error_type,
                                     int errornum,
                                     void *user_data)
{
    (void) user_data;

    switch (error_type) 
    {
        case ILM_ERROR_WAYLAND:
        {
            printf("LayerManager: exit, ilm shutdown due to wayland error: %s\n",
                   strerror(errornum));
            break;
        }
        case ILM_ERROR_POLL:
        {
            printf("LayerManager: exit, ilm shutdown due to poll error: %s\n",
                   strerror(errornum));
            break;
        }
        default:
        {
            printf("LayerManager: exit, ilm shutdown due to unknown reason: %s\n",
                   strerror(errornum));
        }
    }

    exit(1);
}


int Init()
{
    pthread_mutexattr_t a;
    if (pthread_mutexattr_init(&a) != 0)
       return -1;

    if (pthread_mutexattr_settype(&a, PTHREAD_MUTEX_RECURSIVE) != 0)
    {
       pthread_mutexattr_destroy(&a);
       return -1;
    }

    if (pthread_mutex_init(&mutex, &a) != 0)
    {
        pthread_mutexattr_destroy(&a);
        fprintf(stderr, "LayerManager: failed to initialize pthread_mutex\n");
        return -1;
    }

    pthread_mutexattr_destroy(&a);


	if (ilm_init() == ILM_FAILED) 
	{
        fprintf(stderr, "LayerManager: ilm_init failed\n");
        return -1;
    }

    ilm_registerShutdownNotification(shutdownCallbackFunction, NULL);
}

void GetScreenInfo()
{
    struct ilmScreenProperties screenProperties;
	ilmErrorTypes callResult = ilm_getPropertiesOfScreen(screen_ID, &screenProperties);
	if (ILM_SUCCESS == callResult)
	{
		screenWidth = screenProperties.screenWidth;
		screenHeight = screenProperties.screenHeight;
	}

}

void CreateScreenLayers()
{
	t_ilm_layer renderOrder[2];
    renderOrder[0] = layer;

    ilm_layerCreateWithDimension(&layer, screenWidth, screenHeight);
    ilm_layerSetVisibility(layer,ILM_TRUE);
    ilm_displaySetRenderOrder(screen_ID, renderOrder, 1);
  
    printf("LayerManager: Created Layer (%d) with Destination Region: x:0 y:0 w:%u h:%u\n", layer, screenWidth, screenHeight);
    ilm_commitChanges();
}

int main (int argc, char *argv[])
{
	// Init Mutex and ILM Interface
    Init();

	// Get Screen Info (Single Screen Support Only)
	GetScreenInfo();

	// Create Default Screen Layers
	CreateScreenLayers();

	ilm_registerNotification(callbackFunction, NULL);

    while (number_of_surfaces > 0) 
	{
        pthread_mutex_lock(&mutex);
        pthread_cond_wait( &waiterVariable, &mutex);
    }

    ilm_unregisterNotification();
    ilm_destroy();

    return 0;
}
