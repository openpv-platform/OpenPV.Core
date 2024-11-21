#include <stdio.h>
#include <unistd.h>
#include <dlfcn.h>
#include <stdlib.h>

#include <GLES2/gl2.h>
#include <EGL/egl.h>
#include <weston-platform.h>

#include "GLSetup.h"
#include "WaylandSetup.h"
#include "DrawingWindow.h"

#ifdef ISPRO
#include <LayerManagerServiceClient.h>
#include <ilm/ilm_control.h>
#include <ilm/ilm_types.h>
#include <ilm/ivi-application-client-protocol.h>
#endif

static DrawingWindowContext context = { 0 };

int InitWindowWithSurfaceId(int surfaceId, char* appId)
{
	// Set to Defaults to allow Surface Lookup
	context.surfaceId = surfaceId;
	context.width = 0;
	context.height = 0;
	context.fullscreen = 0;

	// Passing No Size assumes Layout Manager will be Sizing the Window.
	return InitWindowCore(surfaceId, appId);
}

int InitWindowWithSize(bool fullScreen, int w, int h, int surfaceId, char* appId)
{
	// Grab Requested Size to override Surface Setup
	context.surfaceId = surfaceId;
	context.fullscreen = (int)fullScreen;
	context.width = fullScreen ? 0 : w;
	context.height = fullScreen ? 0 : h;

	// When using IVI Shell and Overriding the Size
	// we send the layout to Layout Service
	if (surfaceId > 0 && !fullScreen)
	{
		UpdateSurfaceLocation(surfaceId, 0, 0, w, h,false);
		UpdateSurfaceSource(surfaceId, 0, 0, w, h, true);
	}
	return InitWindowCore(surfaceId, appId);
}

int UpdateSurfaceLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges)
{
	// Grab Requested Size
	#ifdef ISPRO
		
		// Send Info to LayerManager to Position Screen
		SurfaceRectangle rectangle;
		rectangle.set_surface_id(surfaceId);
		rectangle.set_x(x);
		rectangle.set_y(y);
		rectangle.set_height(h);
		rectangle.set_width(w);
		rectangle.set_commit_changes(commitChanges);
		
		LayerManagerServiceClient& serviceClient = LayerManagerServiceClient::GetDefaultClient();
		serviceClient.UpdateSurfaceLocation(rectangle);
	#endif

	return 1;
}

int UpdateSurfaceSource(int surfaceId, int x, int y, int w, int h, bool commitChanges)
{
	// Grab Requested Size
	#ifdef ISPRO
		
		// Send Info to LayerManager to Position Screen
		SurfaceRectangle rectangle;
		rectangle.set_surface_id(surfaceId);
		rectangle.set_x(x);
		rectangle.set_y(y);
		rectangle.set_height(h);
		rectangle.set_width(w);
		rectangle.set_commit_changes(commitChanges);
		
		LayerManagerServiceClient& serviceClient = LayerManagerServiceClient::GetDefaultClient();
		serviceClient.UpdateSurfaceSource(rectangle);
	#endif

	return 1;
}

int UpdateSurfaceVisibility(int surfaceId, bool visibility, float opacity, bool commitChanges)
{
	// Grab Requested Size
	#ifdef ISPRO
		
		// Send Info to LayerManager to Position Screen
		SurfaceVisibility visInfo;
		visInfo.set_surface_id(surfaceId);
		visInfo.set_is_visible(visibility);
		visInfo.set_opacity(opacity);
		visInfo.set_commit_changes(commitChanges);
		
		LayerManagerServiceClient& serviceClient = LayerManagerServiceClient::GetDefaultClient();
		serviceClient.UpdateSurfaceVisibility(visInfo);
	#endif
	return 1;
}

int InitWindowCore(int surfaceId, char* appId)
{
	// Load Surface Config if Found
	#ifdef ISPRO
		LayerManagerConfiguration config;
		AhsokaLayerManager::LayerManagerServiceClient::LoadLayerManagerConfig(config);
		for (SurfaceSetup setup : config.surface_setup())
			if (setup.surface_id() == surfaceId && context.width == 0 && context.height == 0)
			{
				printf("Found Surface Configuration - Using Size Info\n");
				context.height = setup.location_rectangle().height();
				context.width = setup.location_rectangle().width();
			}
	#endif

	// Width and Height was 0 (Failed Lookup)...so fall back to full Screen	
	if (context.width == 0 || context.height == 0) 
	{
		// Failed Surface Lookup
		printf("No Surface Or Size Found - Defaulting to Full Screen\n");
		context.fullscreen = 1;
	}

	// Grab AppID / AppName
	context.surfaceId = surfaceId;
	context.appId = (char*)malloc(strlen(appId));
	strcpy(appId,context.appId);

	context.initialized = false;
	
	for (size_t i = 0; i < MAX_SEATS; i++)
	{
		context.seats[i].pointer = 0;
		context.seats[i].keyboard = 0;
		context.seats[i].touch = 0;
	}

//Ask the IVI Shell for the Window Size if running Pro
#ifdef ISPRO
	ilm_init();
	t_ilm_display screenid = 0;
	struct ilmScreenProperties screenProperties;

	ilmErrorTypes callResult = ilm_getPropertiesOfScreen(screenid, &screenProperties);
	if (ILM_SUCCESS == callResult)
	{
		if (context.fullscreen)
		{
			context.width = screenProperties.screenWidth;
			context.height = screenProperties.screenHeight;
		}
	}
	ilm_destroy();
#endif

	int ret = InitWaylandClient(&context);
	if (ret != 0)
		return -1;

	ret = InitEGL(&context);
	if (ret != 0)
		return -1;

#ifndef ISPRO
	ret = CreateXdgLayer(&context);
	if (ret != 0)
		return -1;
#elif ISPRO
	context.surface = wl_compositor_create_surface(context.compositor);
  	struct ivi_surface * surface = ivi_application_surface_create(context.ivi_app, surfaceId, context.surface);
	wl_display_roundtrip(context.display);
#endif

	// Create GL
	CreateEglWindow(&context);
	
	// start the touch input queue
	createXdgTouchQueue();

	context.surface_height = context.height;
	context.surface_width = context.width;
	context.initialized = true;
	
	if (CAIRO_DEBUG) printf("-- Done with window interop INIT...\r\n");

	pthread_t dispatch_thread;
	ret = pthread_create(&dispatch_thread, NULL, &WaylandDispatchThread, NULL);
	if (ret != 0)
		return -1;
	
	if (CAIRO_DEBUG) printf("-- Dispatch thread created successfully...\r\n");
	
	return 0;
}

void GetWindowDimensions(int *w, int *h)
{
	*w = context.width;
	*h = context.height;
}

void GetSurfaceInfo(int *samples, int *stencilBits, unsigned int * gl_format)
{
	int val = 0;
	eglGetConfigAttrib(context.egl.dpy, context.egl.conf, EGL_SAMPLES, &val);
	*samples = val;

	eglGetConfigAttrib(context.egl.dpy, context.egl.conf, EGL_STENCIL_SIZE, &val);
	*stencilBits = val;

	*gl_format = (int)0x8058; // GL_RGBA8 
}

bool GetTouchData(TouchEvent *data, int timeoutInMs)
{
	bool result = false;
	result = dequeue(data, timeoutInMs);
	return result;
}

void* WaylandDispatchThread(void *)
{
	// Background thread to call dispatch and block until incoming events are processed
	while (1)
	{
		wl_display_dispatch(context.display);
	}
	return NULL;
}


void* GetFunction(const char *name)
{
	return (void*)eglGetProcAddress(name);;
}

void GLSwapBuffers()
{
	eglSwapBuffers(context.egl.dpy, context.egl_surface);
}

bool UpdateSwapInterval(int interval)
{
	return eglSwapInterval(context.egl.dpy, interval);
}

void DestoryWindowContext()
{
	eglMakeCurrent(context.egl.dpy, EGL_NO_SURFACE, EGL_NO_SURFACE, EGL_NO_CONTEXT);

	weston_platform_destroy_egl_surface(context.egl.dpy,context.egl_surface);
	wl_egl_window_destroy(context.native);

	if (context.xdg_toplevel)
		xdg_toplevel_destroy(context.xdg_toplevel);
	if (context.xdg_surface)
		xdg_surface_destroy(context.xdg_surface);

	wl_surface_destroy(context.surface);

	if (context.callback)
		wl_callback_destroy(context.callback);

	if (context.shm)
		wl_shm_destroy(context.shm);

	for (size_t i = 0; i < MAX_SEATS; i++)
	{
		if (context.seats[i].seat != 0)
		{
			if (context.seats[i].pointer)
				wl_pointer_destroy(context.seats[i].pointer);

			if (context.seats[i].keyboard)
				wl_keyboard_destroy(context.seats[i].keyboard);

			if (context.seats[i].touch)
				wl_touch_destroy(context.seats[i].touch);

			if (context.seats[i].seat)
				wl_seat_destroy(context.seats[i].seat);
		}
	}

	if (context.wm_base)
		xdg_wm_base_destroy(context.wm_base);

	if (context.compositor)
		wl_compositor_destroy(context.compositor);

	wl_registry_destroy(context.registry);
	wl_display_flush(context.display);
	wl_display_disconnect(context.display);
}
