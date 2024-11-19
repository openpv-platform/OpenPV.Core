#include <stdio.h>
#include <unistd.h>

#include "DrawingWindow.h"
#include "WaylandSetup.h"
#include "GLSetup.h"
#include <GLES2/gl2.h>
#include <EGL/egl.h>
#include "weston-platform.h"
#include <dlfcn.h>
#include <stdlib.h>
#include <LayerManagerConfigReader.h>

#ifdef ISPRO
#include <LayerManagerServiceClient.h>
#include "ilm/ilm_control.h"
#include "ilm/ilm_types.h"
#include <ilm/ivi-application-client-protocol.h>
#endif


static DrawingWindowContext context = { 0 };

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

void* wayland_dispatch_thread(void *)
{
	// Background thread to call dispatch and block until incoming events are processed
	while (1)
	{
		wl_display_dispatch(context.display);
	}
	return NULL;
}

int InitWindowWithSurfaceId(int surfaceId, char* appId)
{
	// Grab Requested Size
	context.surfaceId = surfaceId;
	context.width = 0;
	context.height = 0;
	context.x = 0;
	context.y = 0;
	context.fullscreen = 0;

	// Passing No Size assumes Layout Manager will be Sizing the Window.

	return InitWindowCore(appId, surfaceId);
}

void SendLayoutToLayoutManager()
{
	#ifdef ISPRO
		// Send Info to LayerManager to Position Screen
		SurfaceLayout layout;
		layout.set_surface_id(context.surfaceId);
		layout.set_height(context.height);
		layout.set_width(context.width);
		layout.set_y(context.y);
		layout.set_x(context.x);
		layout.set_is_visible(true);
		
		LayerManagerServiceClient& serviceClient = LayerManagerServiceClient::GetDefaultClient();
		serviceClient.UpdateSurfaceLayout(layout);
	#endif
}

int InitWindowWithSize(bool fullScreen,int x, int y, int w, int h, char* appId, int surfaceId)
{
	// Grab Requested Size
	context.surfaceId = surfaceId;
	context.width = w;
	context.height = h;
	context.x = x;
	context.y = y;
	context.fullscreen = (int)fullScreen;
	
	// When using IVI Shell and Overriding the Size
	// we send the layout to Layout Service
	if (surfaceId > 0 && !fullScreen)
		SendLayoutToLayoutManager();

	return InitWindowCore(appId, surfaceId);
}

int UpdateWindowPosition(int surfaceId, int x, int y, int w, int h)
{
	// Grab Requested Size
	context.width = w;
	context.height = h;
	context.x = x;
	context.y = y;

	SendLayoutToLayoutManager();

	return 1;
}



int InitWindowCore(char* appId, int surfaceId)
{
	// Load Surface Config
	AhsokaLayerManager::ConfigReader::LoadLayerManagerConfig();
	auto surfaceInfo = AhsokaLayerManager::ConfigReader::GetSurfaceLayout(surfaceId);
	if (surfaceInfo.surface_id() == surfaceId && context.width == 0 && context.height == 0)
	{	
		printf("Found Surface Configuration\n");
		context.x = surfaceInfo.x();
		context.y = surfaceInfo.y();
		context.height = surfaceInfo.height();
		context.width = surfaceInfo.width();
	}
	else if (context.width == 0 || context.height == 0) 
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
	ret = pthread_create(&dispatch_thread, NULL, &wayland_dispatch_thread, NULL);
	if (ret != 0)
		return -1;
	
	if (CAIRO_DEBUG) printf("-- Dispatch thread created successfully...\r\n");
	
	return 0;
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
