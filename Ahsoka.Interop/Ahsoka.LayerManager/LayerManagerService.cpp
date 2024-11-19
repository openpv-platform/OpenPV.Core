#include <string>
#include <functional>
#include <memory>
#include <vector>
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaServiceEndPoint.h"
#include "LayerManagerServiceMessages.h"

#include "LayerManagerConfigReader.h"
#include "LayerManagerService.h"

#include <pthread.h>
#include <wayland-client.h>
#include <wayland-server.h>
#include "ilm/ilm_control.h"
#include <ilm/ivi-application-client-protocol.h>

using namespace std;
using namespace AhsokaSystem;
using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{

    // Static Members
    ReadyWaiter LayerManagerService::waitForExit;
    t_ilm_uint LayerManagerService::layer = 1000;
    t_ilm_uint LayerManagerService::screen_ID = 0;
    t_ilm_uint LayerManagerService::screenWidth;
    t_ilm_uint LayerManagerService::screenHeight;
    bool LayerManagerService::showUnknownSurfaces = true;
    std::vector<int> LayerManagerService::surfaceList;

    LayerManagerService::LayerManagerService():
        AhsokaServiceFramework::AhsokaServiceEndPoint<LayoutManagerMessageTypes_Ids>(localConfig, LayerManagerServiceMessages()) 
    {

    }	

    
    void LayerManagerService::StartAndRun()
    {

        cout << "LayerManager: Startup" << std::endl;
        if (ilm_init() == ILM_FAILED) 
        {
            cout << "LayerManager: ilm_init failed " << std::endl;
            return;
        }

        // Init Mutex and ILM Interface
        ilm_registerShutdownNotification(WaylandShutdown, NULL);

        // Load Layer Manager Configuration
        auto configuration = ConfigReader::LoadLayerManagerConfig();
        showUnknownSurfaces = configuration.show_unknown_surfaces();

        // Get Screen Info (Single Screen Support Only)
        // Creates Screen and Layer 1000
        CreateScreen();

        // Starts AhsokaService
        StartLayoutService();

        // register signal SIGINT and signal handler  
        signal(SIGTERM, RequestShutdown);

        // Wait for Exit
        waitForExit.WaitForReady();

        // Stop Service
        Stop();
        
        // Unregister Notifications and Close ILM
        ilm_unregisterNotification();
        ilm_destroy();
        
        cout << "LayerManager: Shutdown Complete " << std::endl;
    }

    void LayerManagerService::StartLayoutService()
    {
        // Wire Up Callbacks for Screen
        ilm_registerNotification(LayerManagerCallback, NULL);

        // Load Service Configuration
        auto config = ConfigReader::GetServiceConfiguration();
        UpdateConfiguration(config);

        // Start the Service Runtime
        Start();
    }

    void LayerManagerService::RequestShutdown(int signum)
    {
        cout << "LayerManager: Shutdown Request Recieved." << std::endl;
        waitForExit.SetReady();
    }

    void LayerManagerService::OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message)
    {
        if (messageHeader.transport_id() == LayoutManagerMessageTypes::CLOSE_LOGO)
        {
            cout << "LayerManager: Close Logo Received" << std::endl;

            // Handle Close Logo
            SendMessageInternal(messageHeader, &message, false);
        }
        else if (messageHeader.transport_id() == LayoutManagerMessageTypes::UPDATE_SURFACE_LAYOUT)
        {
            auto surfaceInfo = (AhsokaLayerManager::SurfaceLayout&)message;
            
            cout << "LayerManager: Update Surface Layout Received for Surface " << surfaceInfo.surface_id()  << std::endl;
           
            // Update Surface Layout Info
            ConfigReader::UpdateSurfaceLayout(surfaceInfo);

            // Reconfigure Surface if its been created...otherwise it will 
            // be configured on creation.
            std::vector<int>::iterator position = std::find(surfaceList.begin(), surfaceList.end(), surfaceInfo.surface_id());
            if (position != surfaceList.end()) 
                ConfigureSurface(surfaceInfo.surface_id(), surfaceInfo.width(), surfaceInfo.height());
     
            // Reply to Caller
            SendMessageInternal(messageHeader, &message, false);
        }
    }


    void LayerManagerService::CreateScreen()
    {
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
   
        t_ilm_layer renderOrder[2];
        renderOrder[0] = layer;

        callResult = ilm_layerCreateWithDimension(&layer, screenWidth, screenHeight);
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


    void LayerManagerService::ConfigureSurface(t_ilm_uint id, t_ilm_uint aWidth, t_ilm_uint aHeight)
    {
        int x = 0;
        int y = 0;
        int height = aHeight;
        int width = aWidth;
        bool visible = showUnknownSurfaces;
        auto layout = ConfigReader::GetSurfaceLayout(id);
        
        if (layout.surface_id() == id)
        {
            printf("LayerManager: Surface Layout Found (%u) Configured with:\n"
            "    x:%u y:%u w:%u h:%u\n", id, layout.x(), layout.y(), layout.width(), layout.height());

            x = layout.x();
            y = layout.y();
            height = layout.height();
            width = layout.width();

            // Is Sizing Surface Differently capture Surface Rect
            // this is usefull for stretching / shrinking window relative to drawing surface
            if (layout.surface_height() > 0 && layout.surface_width() > 0)
            {
                aHeight = layout.surface_height();
                aWidth = layout.surface_width();
            }

            visible = layout.is_visible();
        }

        ilm_surfaceSetDestinationRectangle(id, x, y, width, height);
        ilm_surfaceSetSourceRectangle(id, 0, 0, aWidth, aHeight);
        ilm_surfaceSetVisibility(id, visible);
        ilm_layerAddSurface(layer,id);
        ilm_surfaceRemoveNotification(id);

        ilm_commitChanges();
        
        printf("LayerManager: Surface (%u) Configured with:\n"
            "    dst region: x:%u y:%u w:%u h:%u\n"
            "    src region: x:%u y:%u w:%u h:%u\n"
            "    visibility: TRUE\n"
            "    added to layer (%u)\n", id, x, y, width, height, 0, 0, aWidth, aHeight,layer);
    }

    void LayerManagerService::SurfaceCallback(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m)
    {
        if ((unsigned)m & ILM_NOTIFICATION_CONFIGURED)
        {
            ConfigureSurface(id, sp->origSourceWidth, sp->origSourceHeight);
        }
    }

    void LayerManagerService::LayerManagerCallback(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data)
    {
        (void)user_data;
        struct ilmSurfaceProperties sp;

        if (object == ILM_SURFACE) 
        {
            if (created) 
            {
                printf("LayerManager: Surface (%d) Created\n",id);
                
                // Add Surface to List
                std::vector<int>::iterator position = std::find(surfaceList.begin(), surfaceList.end(), id);
                if (position == surfaceList.end()) 
                    surfaceList.push_back(id);

                // always get configured event to follow the surface changings
                ilm_surfaceAddNotification(id, &SurfaceCallback);
                ilm_commitChanges();
                ilm_getPropertiesOfSurface(id, &sp);

                if ((sp.origSourceWidth != 0) && (sp.origSourceHeight !=0))
                {   
                    // surface is already configured
                    ConfigureSurface(id, sp.origSourceWidth, sp.origSourceHeight);
                }
            }
            else if (!created)
            {
                // Remove from List
                std::vector<int>::iterator position = std::find(surfaceList.begin(), surfaceList.end(), id);
                if (position != surfaceList.end()) 
                    surfaceList.erase(position);

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

    void LayerManagerService::WaylandShutdown(t_ilm_shutdown_error_type error_type,
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

}