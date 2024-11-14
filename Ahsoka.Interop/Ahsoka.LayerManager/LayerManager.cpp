#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <pthread.h>
#include <getopt.h>
#include <time.h>

#include <wayland-client.h>
#include <wayland-server.h>
#include <ilm/ilm_control.h>
#include "LayerManager.h"
#include "LayerManagerConfigReader.h"

typedef struct 
{
	int x;
	int y;
} SizeInfo;

// We Default to Layer 1000 and Screen 0
t_ilm_uint layer = 1000;
t_ilm_uint screen_ID = 0;
t_ilm_uint screenWidth;
t_ilm_uint screenHeight;

pthread_mutex_t mutex;
static pthread_cond_t  waiterVariable = PTHREAD_COND_INITIALIZER;
static int number_of_surfaces = 100;

void configure_ilm_surface(t_ilm_uint id, t_ilm_uint aWidth, t_ilm_uint aHeight)
{
    int x = 0;
    int y = 0;
    int height = aHeight;
    int width = aWidth;
    auto layout = GetSurfaceLayout(id);
    if (layout.surface_id() == id)
    {
        printf("LayerManager: Surface Layout Found (%u) Configured with:\n"
           "    x:%u y:%u w:%u h:%u\n", id, layout.x(), layout.y(), layout.width(), layout.height());

        x = layout.x();
        y = layout.y();
        height = layout.height();
        width = layout.width();
    }

    ilm_surfaceSetDestinationRectangle(id, x, y, width, height);
    ilm_surfaceSetSourceRectangle(id, 0, 0, aWidth, aHeight);
    ilm_surfaceSetVisibility(id, ILM_TRUE);
    ilm_layerAddSurface(layer,id);
    ilm_surfaceRemoveNotification(id);

    ilm_commitChanges();
    pthread_cond_signal( &waiterVariable );
    
    printf("LayerManager: Surface (%u) Configured with:\n"
           "    dst region: x:%u y:%u w:%u h:%u\n"
           "    src region: x:%u y:%u w:%u h:%u\n"
           "    visibility: TRUE\n"
           "    added to layer (%u)\n", id, x, y, width, height, 0, 0, aWidth, aHeight,layer);
}

void surfaceCallbackFunction(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m)
{
    if ((unsigned)m & ILM_NOTIFICATION_CONFIGURED)
    {
        configure_ilm_surface(id, sp->origSourceWidth, sp->origSourceHeight);
    }
}

void callbackFunction(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data)
{
    (void)user_data;
    struct ilmSurfaceProperties sp;

    if (object == ILM_SURFACE) 
	{
        if (created) 
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
        else if (!created)
        {
            number_of_surfaces++;
            printf("LayerManager: Surface (%u) Destroyed\n",id);
        }
    } 
	else if (object == ILM_LAYER) 
	{
        if (created)
            printf("LayerManager: Layer (%u) Created\n",id);
        else if(!created)
            printf("LayerManager: Layer (%u) Destroyed\n",id);
    }
}

void shutdownCallbackFunction(t_ilm_shutdown_error_type error_type,
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

    LoadLayerManagerConfig();

    return 0;
}

void GetScreenInfo()
{
	printf("Getting Screen Info\n");
	
	struct ilmScreenProperties screenProperties;
	screenProperties.screenWidth = 0;
	screenProperties.screenHeight = 0;

	ilmErrorTypes callResult = ilm_getPropertiesOfScreen(screen_ID, &screenProperties);
	
	while (ILM_SUCCESS != callResult || screenProperties.screenWidth == 0)
	{
		printf("Waiting for Screen Properties\n");
		usleep(100*1000);
		callResult = ilm_getPropertiesOfScreen(screen_ID, &screenProperties);
	}
 	
	screenWidth = screenProperties.screenWidth;
	screenHeight = screenProperties.screenHeight;
}

void CreateScreenLayers()
{
    t_ilm_layer renderOrder[2];
    renderOrder[0] = layer;

    ilmErrorTypes callResult = ilm_layerCreateWithDimension(&layer, screenWidth, screenHeight);
    if (callResult != ILM_SUCCESS)
       printf("Error ilm_layerCreateWithDimension in CreateScreenLayers\n");
  
    callResult = ilm_layerSetVisibility(layer,ILM_TRUE);
    if (callResult != ILM_SUCCESS)
       printf("Error ilm_layerSetVisibility in CreateScreenLayers\n");
    
    callResult = ilm_displaySetRenderOrder(screen_ID, renderOrder, 1);
    if (callResult != ILM_SUCCESS)
       printf("Error ilm_displaySetRenderOrder in CreateScreenLayers\n");
  
    ilm_commitChanges();
    printf("LayerManager: Created Layer (%d) with Destination Region: x:0 y:0 w:%u h:%u\n", layer, screenWidth, screenHeight);
  
}

void AppRun()
{
    ilm_registerNotification(callbackFunction, NULL);

    while (number_of_surfaces > 0) 
	{
        pthread_mutex_lock(&mutex);
        pthread_cond_wait( &waiterVariable, &mutex);
    }

    ilm_unregisterNotification();
    ilm_destroy();
}