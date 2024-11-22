

void HandleGlobalRegistry(void *data, struct wl_registry *registry,
		       uint32_t name, const char *interface, uint32_t version)
{

}


void InitLayerManager(DrawingWindowContext& context)
{
	context.registryCallback = HandleGlobalRegistry;
}

void CreateLayoutSurface(DrawingWindowContext& context)
{
	CreateXdgLayer(&context);
}

int UpdateSurfaceLocationImpl(int surfaceId, int x, int y, int w, int h, bool commitChanges)
{
	return 1;
}

int UpdateSurfaceSourceImpl(int surfaceId, int x, int y, int w, int h, bool commitChanges)
{
	return 1;
}

int UpdateSurfaceVisibilityImpl(int surfaceId, bool visibility, float opacity, bool commitChanges)
{
	return 1;
}

void CloseLayerManager(){}