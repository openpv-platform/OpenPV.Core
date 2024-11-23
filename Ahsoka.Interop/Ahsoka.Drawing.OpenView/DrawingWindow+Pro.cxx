#include <LayerManagerServiceClient.h>
#include <ilm/ilm_control.h>
#include <ilm/ilm_types.h>
#include <ilm/ivi-application-client-protocol.h>

// Start Surfaces at 20000
int lastSurfaceId = 20000;

void HandleGlobalRegistry(void *data, struct wl_registry *registry,
		       uint32_t name, const char *interface, uint32_t version)
{
	DrawingWindowContext *d = (DrawingWindowContext *) data;
	
	if (strcmp(interface, "ivi_application") == 0) 
	{
        d->ivi_app =  (struct ivi_application*)wl_registry_bind(registry, name, &ivi_application_interface, 1);
    }
}


void InitLayerManager(DrawingWindowContext& context)
{
	context.registryCallback = HandleGlobalRegistry;

	LayerManagerConfiguration config;
	AhsokaLayerManager::LayerManagerServiceClient::LoadLayerManagerConfig(config);
	for (SurfaceSetup setup : config.surface_setup())
		if (setup.surface_id() == context.surfaceId && context.width == 0 && context.height == 0)
		{
			printf("Found Surface Configuration - Using Size Info\n");
			context.height = setup.location_rectangle().height();
			context.width = setup.location_rectangle().width();
		}

	ilm_init();
		
	// Use ilm to get screen size if needed
	if (context.fullscreen || context.height == 0  || context.width == 0)
	{		
		//Ask the IVI Shell for the Window Size if running Pro
		t_ilm_display screenid = 0;
		struct ilmScreenProperties screenProperties;

		ilmErrorTypes callResult = ilm_getPropertiesOfScreen(screenid, &screenProperties);
		if (ILM_SUCCESS == callResult)
		{
			context.width = screenProperties.screenWidth;
			context.height = screenProperties.screenHeight;
			context.fullscreen = 1;
		}
	}

	if (context.surfaceId == -1)
	{
        //surfaces on layer (in rendering order)
        int surfaceCount = 0;
        t_ilm_surface* surfaceArray = NULL;

        ilmErrorTypes  callResult = ilm_getSurfaceIDs(&surfaceCount, &surfaceArray);
        if (ILM_SUCCESS != callResult)
        {
            cout << "LayerManagerService: ilm_getSurfaceIDs returned: " << ILM_ERROR_STRING(callResult) << "\n";
            return;
        }

		bool found = false;
		do
		{
			found = false;
			for (size_t i = 0; i < surfaceCount; i++)
				if (surfaceArray[i] == lastSurfaceId)
				{	
					found = true;
					break;
				}

			lastSurfaceId++;
		} 
		while (found);

		context.surfaceId = lastSurfaceId;
		
		free(surfaceArray);
	}
		
	ilm_destroy();
}

void CreateLayoutSurface(DrawingWindowContext& context)
{
	context.surface = wl_compositor_create_surface(context.compositor);
  	struct ivi_surface * surface = ivi_application_surface_create(context.ivi_app, context.surfaceId, context.surface);
	wl_display_roundtrip(context.display);
}

int UpdateSurfaceLocationImpl(int surfaceId, int x, int y, int w, int h, bool commitChanges)
{
	// Send Info to LayerManager to Position Screen
	SurfaceRectangle rectangle;
	rectangle.set_surface_id(surfaceId);
	rectangle.set_x(x);
	rectangle.set_y(y);
	rectangle.set_height(h);
	rectangle.set_width(w);
	rectangle.set_commit_changes(commitChanges);
	
	LayerManagerServiceClient* serviceClient = LayerManagerServiceClient::GetDefaultClient();
	serviceClient->UpdateSurfaceLocation(rectangle);

	return 1;
}

int UpdateSurfaceSourceImpl(int surfaceId, int x, int y, int w, int h, bool commitChanges)
{
	// Send Info to LayerManager to Position Screen
	SurfaceRectangle rectangle;
	rectangle.set_surface_id(surfaceId);
	rectangle.set_x(x);
	rectangle.set_y(y);
	rectangle.set_height(h);
	rectangle.set_width(w);
	rectangle.set_commit_changes(commitChanges);
	
	LayerManagerServiceClient* serviceClient = LayerManagerServiceClient::GetDefaultClient();
	serviceClient->UpdateSurfaceSource(rectangle);

	return 1;
}

int UpdateSurfaceVisibilityImpl(int surfaceId, bool visibility, float opacity, bool commitChanges)
{
	// Grab Requested Size
	// Send Info to LayerManager to Position Screen
	SurfaceVisibility visInfo;
	visInfo.set_surface_id(surfaceId);
	visInfo.set_is_visible(visibility);
	visInfo.set_opacity(opacity);
	visInfo.set_commit_changes(commitChanges);
	
	LayerManagerServiceClient* serviceClient = LayerManagerServiceClient::GetDefaultClient();
	serviceClient->UpdateSurfaceVisibility(visInfo);

	return 1;
}

void CloseLayerManager()
{
	// Stop Client if Running
	auto client = LayerManagerServiceClient::GetDefaultClient(false);
	if (client != 0 && client->GetIsConnected())
	{
		std::cout << "Stopping Service Client" << std::endl;
		client->Stop();
	}
}