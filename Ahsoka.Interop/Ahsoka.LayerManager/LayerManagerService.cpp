#include <string>
#include <functional>
#include <memory>
#include <vector>

#include "AhsokaRuntimeBuilder.h"
#include "AhsokaServiceEndPoint.h"
#include "LayerManagerServiceMessages.h"
#include "LayerManagerService.h"
#include "LayerManagerServiceClient.h"

#include <pthread.h>
#include <wayland-client.h>
#include <wayland-server.h>
#include "ilm/ilm_control.h"
#include <ilm/ivi-application-client-protocol.h>

using namespace std;
using namespace AhsokaCore;
using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{
    // Static Members
    LayerManagerService::LayerManagerContext LayerManagerService::context;

    LayerManagerService::LayerManagerService():
        AhsokaServiceFramework::AhsokaServiceEndPoint<LayerManagerMessageTypes_Ids>(LayerManagerServiceClient::GetServiceConfiguration(), LayerManagerServiceMessages()) 
    {

    }	
    
    void LayerManagerService::StartAndRun()
    {
        context.layerId = 1000;
        context.screenId = 0;

        if (ilm_init() == ILM_FAILED) 
        {
            cout << "LayerManager: ilm_init failed " << std::endl;
            return;
        }

        // Init Mutex and ILM Interface
        ilm_registerShutdownNotification(WaylandShutdown, NULL);

        // Load Layer Manager Configuration
        LayerManagerServiceClient::LoadLayerManagerConfig(context.layerConfiguration);
        
        // Get Screen Info (Single Screen Support Only)
        // Creates Screen and Layer 1000
        CreateScreen();

        // Starts AhsokaService
        StartLayoutService();

        // register signal SIGINT and signal handler  
        signal(SIGTERM, RequestShutdown);

        // Wait for Exit
        context.waitForExit.WaitForReady();

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
        ilm_registerNotification(LayerCallback, NULL);

        // Start the Service Runtime
        Start();
    }

    void LayerManagerService::RequestShutdown(int signum)
    {
        cout << "LayerManager: Shutdown Request Recieved." << std::endl;
        context.waitForExit.SetReady();
    }

    bool LayerManagerService::IsSurfaceActive(int surfaceId)
    {
        auto position = std::find(context.surfaceList.begin(), context.surfaceList.end(), surfaceId);
        return (position != context.surfaceList.end());
    }

    void LayerManagerService::OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message)
    {
        if (messageHeader.transport_id() == LayerManagerMessageTypes::STOP_LOGO)
        {
            // Handle Close Logo
            SendMessageInternal(messageHeader, &message, false);
        }
        else if (messageHeader.transport_id() == LayerManagerMessageTypes::UPDATE_SURFACE_SETUP)
        {
            auto surfaceInfo = (AhsokaLayerManager::SurfaceSetup&)message;
            
            // Update Surface Layout Info
            UpdateSurfaceSetup(surfaceInfo);

            // Reconfigure Surface if its been created...otherwise it will 
            // be configured on creation.
            if (IsSurfaceActive(surfaceInfo.surface_id()))
                ConfigureSurface(surfaceInfo.surface_id(), surfaceInfo.location_rectangle().width(), surfaceInfo.location_rectangle().height());
          
            // Reply to Caller
            SendMessageInternal(messageHeader, &message, false);
        }
        else if (messageHeader.transport_id() == LayerManagerMessageTypes::UPDATE_SURFACE_LOCATION)
        {
            auto location = (AhsokaLayerManager::SurfaceRectangle&)message;
            auto surfaceInfo = GetSurfaceSetup(location.surface_id());
            
            if (location.surface_id() == surfaceInfo.surface_id())
            {   
                // Update Surface Layout Info
                surfaceInfo.mutable_location_rectangle()->CopyFrom(location);
                UpdateSurfaceSetup(surfaceInfo);

                // Reconfigure Surface if its been created...otherwise it will 
                // be configured on creation.
                if (IsSurfaceActive(surfaceInfo.surface_id()))
                {
                    ilm_surfaceSetDestinationRectangle(location.surface_id(), location.x(), location.y(), location.width(), location.height());
                    if(location.commit_changes())
                        ilm_commitChanges();
                }
            }
            else
               cout << "LayerManager: Location Recieved with No Known Surface " << location.surface_id()  << std::endl;
            

            // Reply to Caller
            SendMessageInternal(messageHeader, &message, false);
        }
        else if (messageHeader.transport_id() == LayerManagerMessageTypes::GET_SURFACE_SETUP)
        {
            auto surfaceId = (AhsokaLayerManager::SurfaceRequest&)message;
            auto surfaceInfo = GetSurfaceSetup(surfaceId.surface_id());
            SendMessageInternal(messageHeader, &surfaceInfo, false);
        }
        else if (messageHeader.transport_id() == LayerManagerMessageTypes::UPDATE_SURFACE_SOURCE)
        {
            auto location = (AhsokaLayerManager::SurfaceRectangle&)message;
            auto surfaceInfo = GetSurfaceSetup(location.surface_id());
            
            if (location.surface_id() == surfaceInfo.surface_id())
            {   
                // Update Surface Layout Info
                surfaceInfo.mutable_source_rectangle()->CopyFrom(location);
                UpdateSurfaceSetup(surfaceInfo);

                // Reconfigure Surface if its been created...otherwise it will 
                // be configured on creation.
                if (IsSurfaceActive(surfaceInfo.surface_id()))
                {
                    ilm_surfaceSetSourceRectangle(location.surface_id(), location.x(), location.y(), location.width(), location.height());
                    if(location.commit_changes())
                        ilm_commitChanges();
              
                }
            }
            else
               cout << "LayerManager: Location Recieved with No Known Surface " << location.surface_id()  << std::endl;
            

            // Reply to Caller
            SendMessageInternal(messageHeader, &message, false);
        }
        else if (messageHeader.transport_id() == LayerManagerMessageTypes::UPDATE_SURFACE_VISIBILITY)
        {
            auto visibility = (AhsokaLayerManager::SurfaceVisibility&)message;
            auto surfaceInfo = GetSurfaceSetup(visibility.surface_id());
            
            if (visibility.surface_id() == surfaceInfo.surface_id())
            {   
                // Update Surface Layout Info
                surfaceInfo.mutable_visibility()->CopyFrom(visibility);
                UpdateSurfaceSetup(surfaceInfo);

                // Reconfigure Surface if its been created...otherwise it will 
                // be configured on creation.
                if (IsSurfaceActive(surfaceInfo.surface_id()))
                {
                    ilm_surfaceSetVisibility(visibility.surface_id(), visibility.is_visible());
                    ilm_surfaceSetOpacity(visibility.surface_id(), visibility.opacity());
                    if(visibility.commit_changes())
                        ilm_commitChanges();
                } 
            }
            else
               cout << "LayerManager: Location Recieved with No Known Surface " << visibility.surface_id()  << std::endl;
            

            // Reply to Caller
            SendMessageInternal(messageHeader, &message, false);
        }
         else if (messageHeader.transport_id() == LayerManagerMessageTypes::UPDATE_SURFACE_Z_ORDER)
        {
            auto surfaceOrder = (AhsokaLayerManager::SurfaceOrder&)message;
            cout << "LayerManager: Update Surface Order to ";
            
            int surfaceCount = surfaceOrder.surface_id().size();
            std::vector<t_ilm_layer> layers;
            for (int i = 0; i < surfaceCount; ++i) 
            {
                int surfaceId = surfaceOrder.surface_id().Get(i);
                if (IsSurfaceActive(surfaceId))
                {    
                    layers.push_back(surfaceId);
                    cout << surfaceId << " ";
                }
                else
                {
                    cout << "Skipped(" << surfaceId << ") ";
                }
            }
            cout << std::endl;

            // Check for Missing SurfaceIDs
            for (const t_ilm_layer& surfaceId : context.surfaceList) 
            {
                auto position = std::find(layers.begin(),layers.end(), surfaceId);
                if (position == layers.end())
                    layers.insert(layers.begin(),surfaceId);
            }

            ilm_layerSetRenderOrder(context.layerId, layers.data(), layers.size());
            if(surfaceOrder.commit_changes())
                ilm_commitChanges();

            // Save New Layer List into Context
            layers.swap(context.surfaceList);
          
            // Reply to Caller
            SendMessageInternal(messageHeader, &message, false);
        }
    }


    void LayerManagerService::CreateScreen()
    {
        struct ilmScreenProperties screenProperties;
        screenProperties.screenWidth = 0;
        screenProperties.screenHeight = 0;

        ilmErrorTypes callResult = ilm_getPropertiesOfScreen(context.screenId, &screenProperties);
        
        while (ILM_SUCCESS != callResult || screenProperties.screenWidth == 0)
        {
            printf("Waiting for Screen Properties\n");
            usleep(100*1000);
            callResult = ilm_getPropertiesOfScreen(context.screenId, &screenProperties);
        }
        
        context.screenWidth = screenProperties.screenWidth;
        context.screenHeight = screenProperties.screenHeight;
   
        t_ilm_layer renderOrder[2];
        renderOrder[0] = context.layerId;

        callResult = ilm_layerCreateWithDimension(&context.layerId, context.screenWidth, context.screenHeight);
        if (callResult != ILM_SUCCESS)
            printf("Error ilm_layerCreateWithDimension in CreateScreenLayers\n");
    
        callResult = ilm_layerSetVisibility(context.layerId,ILM_TRUE);
        if (callResult != ILM_SUCCESS)
            printf("Error ilm_layerSetVisibility in CreateScreenLayers\n");
        
        callResult = ilm_displaySetRenderOrder(context.screenId, renderOrder, 1);
        if (callResult != ILM_SUCCESS)
            printf("Error ilm_displaySetRenderOrder in CreateScreenLayers\n");
    
        ilm_commitChanges();
        printf("LayerManager: Created Layer (%d) with Destination Region: x:0 y:0 w:%u h:%u\n", context.layerId, context.screenWidth, context.screenHeight);
    }

    SurfaceSetup LayerManagerService::GetSurfaceSetup(int surfaceId)
    {
        SurfaceSetup emptyLayout;
        for (SurfaceSetup setup : context.layerConfiguration.surface_setup())
            if (setup.surface_id() == surfaceId)
                return setup;

        emptyLayout.set_surface_id(-12345678); // Unknown Surface
        return emptyLayout;
    }


    void LayerManagerService::UpdateSurfaceSetup(SurfaceSetup& newLayout)
    {
        LayerManagerConfiguration& config = context.layerConfiguration;
        for (size_t i = 0; i < config.surface_setup().size(); i++)
        {
            // Remove Existing Item..if exists
            if (config.surface_setup()[i].surface_id() == newLayout.surface_id())
            {
                config.mutable_surface_setup()->SwapElements(i, config.mutable_surface_setup()->size() - 1);
                config.mutable_surface_setup()->RemoveLast();
            }
        }

        // Add Updated Item to List
        config.mutable_surface_setup()->Add()->CopyFrom(newLayout);
    }

    void LayerManagerService::ValidateSurface(SurfaceSetup& setup, int id, int aRequestedWidth, int aRequestedHeight)
    {
        bool needsUpdate = false;

        // if a Surface is Found Or if the 
        if (setup.surface_id() == id)
        {
            printf("LayerManager: Surface Setup Found (%u)\n", id);

            // If Sizing Surface Differently capture Surface Rect
            // this is usefull for stretching / shrinking window relative to drawing surface
            if (setup.source_rectangle().height() == 0 || setup.source_rectangle().width() == 0)
            {
                // Set Defaults on 
                setup.mutable_source_rectangle()->set_x(setup.location_rectangle().x());
                setup.mutable_source_rectangle()->set_y(setup.location_rectangle().y());
                setup.mutable_source_rectangle()->set_height(setup.location_rectangle().height());
                setup.mutable_source_rectangle()->set_width(setup.location_rectangle().width());
                needsUpdate = true;
            }
        }
        else
        {
            printf("LayerManager: Surface Not Found Found for (%u) - Generating from Request\n", id);

            setup.set_surface_id(id);
          
            auto surfaceLocation = setup.mutable_location_rectangle();
            surfaceLocation->set_surface_id(id);
            surfaceLocation->set_height(aRequestedHeight);
            surfaceLocation->set_width(aRequestedWidth);

            auto surfaceSource = setup.mutable_source_rectangle();
            surfaceSource->set_surface_id(id);
            surfaceSource->set_height(aRequestedHeight);
            surfaceSource->set_width(aRequestedWidth);
           
            auto visibility = setup.mutable_visibility();
            visibility->set_surface_id(id);
            visibility->set_is_visible(context.layerConfiguration.show_unknown_surfaces());
            visibility->set_opacity(1.0);

            needsUpdate = true;
         }

        if (needsUpdate)
            UpdateSurfaceSetup(setup);
    }

    void LayerManagerService::ConfigureSurface(t_ilm_uint id, t_ilm_uint aRequestedWidth, t_ilm_uint aRequestedHeight)
    {
        bool visible = context.layerConfiguration.show_unknown_surfaces();
        
        auto setup = GetSurfaceSetup(id);
        
        if (setup.surface_id() != id)
             ValidateSurface(setup, id, aRequestedWidth, aRequestedHeight);

        ilm_surfaceSetDestinationRectangle(id, setup.location_rectangle().x(), 
                                               setup.location_rectangle().y(), 
                                               setup.location_rectangle().width(), 
                                               setup.location_rectangle().height());

        ilm_surfaceSetSourceRectangle(id, setup.source_rectangle().x(), 
                                               setup.source_rectangle().y(), 
                                               setup.source_rectangle().width(), 
                                               setup.source_rectangle().height());

        ilm_surfaceSetVisibility(id, setup.visibility().is_visible());
        ilm_surfaceSetOpacity(id, setup.visibility().opacity());

        ilm_layerAddSurface(context.layerId,id);
        ilm_layerSetRenderOrder(context.layerId, context.surfaceList.data(),context.surfaceList.size()); // Re-Apply Surface order
        ilm_surfaceRemoveNotification(id); // Stop Listening for Changes
        ilm_commitChanges();
        
        printf("LayerManager: Surface (%u) Configured with:\n"
            "    Location: x:%u y:%u w:%u h:%u\n"
            "    Source: x:%u y:%u w:%u h:%u\n"
            "    Visibility: %u\n"
            "    Opacity: %f\n"
            "    Layer: %u\n", id,  setup.location_rectangle().x(), 
                                    setup.location_rectangle().y(), 
                                    setup.location_rectangle().width(), 
                                    setup.location_rectangle().height(),
                                    setup.source_rectangle().x(), 
                                    setup.source_rectangle().y(), 
                                    setup.source_rectangle().width(), 
                                    setup.source_rectangle().height(), 
                                    setup.visibility().is_visible(), 
                                    setup.visibility().opacity(), 
                                    context.layerId);
    }

    void LayerManagerService::SurfaceCallback(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m)
    {
        if ((unsigned)m & ILM_NOTIFICATION_CONFIGURED)
        {
            ConfigureSurface(id, sp->origSourceWidth, sp->origSourceHeight);
        }
    }

    void LayerManagerService::LayerCallback(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data)
    {
        (void)user_data;
        struct ilmSurfaceProperties sp;

        if (object == ILM_SURFACE) 
        {
            if (created) 
            {
                printf("LayerManager: Surface (%d) Created\n",id);
                
                // Add Surface to List
                std::vector<t_ilm_uint>::iterator position = std::find(context.surfaceList.begin(), context.surfaceList.end(), id);
                if (position == context.surfaceList.end()) 
                    context.surfaceList.push_back(id);

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
                std::vector<t_ilm_uint>::iterator position = std::find(context.surfaceList.begin(), context.surfaceList.end(), id);
                if (position != context.surfaceList.end()) 
                    context.surfaceList.erase(position);

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

        context.waitForExit.SetReady();
    }

}