#include <stdio.h>
#include <unistd.h>



#include "OpenPvGLESContext.h"
#include "WaylandClientInterop.h"
#include "EGLSurface.h"
#include <GLES2/gl2.h>
#include <EGL/egl.h>
#include "weston-platform.h"
#include <dlfcn.h>

#ifdef ISPRO
#include "ilm/ilm_client.h"
#include "ilm/ilm_control.h"
#include "ilm/ilm_types.h"
#include <ilm/ivi-application-client-protocol.h>
#endif

static OpenPvDisplay 		display = { 0 };
static OpenPvWindow 		window  = { 0 };
static OpenPvGLContext 		context = { 0 };

void GetWindowDimensions(int *w, int *h)
{
	*w = window.req_width;
	*h = window.req_height;
}

void GetSurfaceInfo(int *samples, int *stencilBits, unsigned int * gl_format)
{
	int val = 0;
	eglGetConfigAttrib(display.egl.dpy, display.egl.conf, EGL_SAMPLES, &val);
	*samples = val;

	eglGetConfigAttrib(display.egl.dpy, display.egl.conf, EGL_STENCIL_SIZE, &val);
	*stencilBits = val;

	*gl_format = (int)0x8058; // GL_RGBA8 
}

bool GetTouchData(TouchEvent *data, int timeoutInMs)
{
	bool result = false;
	result = dequeue(data, timeoutInMs);
	return result;
}

void* wayland_dispatch_thread()
{
	// Background thread to call dispatch and block until incoming events are processed
	while (1)
	{
		wl_display_dispatch(display.display);
	}
	return NULL;
}

int InitWindowInterop(int x, int y, int w, int h, bool fullScreen)
{
	pthread_t dispatch_thread;
	int ret = 0;
	display.window = &window;
	window.display = &display;
	display.initialized = false;
	window.initialized = false;

	for (size_t i = 0; i < MAX_SEATS; i++)
	{
		display.seats[i].pointer = 0;
		display.seats[i].keyboard = 0;
		display.seats[i].touch = 0;
	}

	window.req_width = w;
	window.req_height = h;

	if (fullScreen)
	{	
		window.fullscreen = 1;
	
#ifdef ISPRO
		ilm_init();
		t_ilm_display screenid = 0;
		struct ilmScreenProperties screenProperties;

		ilmErrorTypes callResult = ilm_getPropertiesOfScreen(screenid, &screenProperties);
		if (ILM_SUCCESS == callResult)
		{
			window.req_width = screenProperties.screenWidth;
			window.req_height = screenProperties.screenHeight;
		}

		ilm_destroy();
#endif
	}
	
	ret = InitWaylandClient(&display, &window);
	if (ret != 0)
		return -1;

	ret = InitEGL(&display, &window);
	if (ret != 0)
		return -1;

	ret = CreateXdgLayer(&display, &window);
	if (ret != 0)
		return -1;

	CreateEglWindow(&display, &window);

	context.display = &display;
	context.window = &window;
	
	// start the touch input queue
	createXdgTouchQueue();

	display.initialized = true;
	window.initialized = true;
	if (CAIRO_DEBUG) printf("-- Done with window interop INIT...\r\n");

	ret = pthread_create(&dispatch_thread, NULL, &wayland_dispatch_thread, NULL);
	if (ret != 0)
		return -1;
	
	if (CAIRO_DEBUG) printf("-- Dispatch thread created successfully...\r\n");
	
	return 0;
}

void * GetFunction(const char *name)
{
	//printf("FOUND FUNCTION - %s %p\r\n", name,symbol);
	void * symbol = eglGetProcAddress(name);
	return symbol;
}

OpenPvDisplay* GetOpenPvDisplay()
{
	if (!display.initialized)
		return NULL;
	
	return &display;
}

EGLDisplay GetGLDisplay()
{
	if (!display.egl.dpy)
		return NULL;
	
	return display.egl.dpy;
}

void GLSwapBuffers()
{
	eglSwapBuffers(display.egl.dpy, window.egl_surface);
}

bool UpdateSwapInterval(int interval)
{
	return eglSwapInterval(display.egl.dpy, interval);
}

void DestoryWindowContext()
{
	eglMakeCurrent(display.egl.dpy, EGL_NO_SURFACE, EGL_NO_SURFACE, EGL_NO_CONTEXT);

	weston_platform_destroy_egl_surface(display.egl.dpy,
					    window.egl_surface);
	wl_egl_window_destroy(window.native);

	if (window.xdg_toplevel)
		xdg_toplevel_destroy(window.xdg_toplevel);
	if (window.xdg_surface)
		xdg_surface_destroy(window.xdg_surface);

	wl_surface_destroy(window.surface);

	if (window.callback)
		wl_callback_destroy(window.callback);

	if (display.shm)
		wl_shm_destroy(display.shm);

	for (size_t i = 0; i < MAX_SEATS; i++)
	{
		if (display.seats[i].seat != 0)
		{
			if (display.seats[i].pointer)
				wl_pointer_destroy(display.seats[i].pointer);

			if (display.seats[i].keyboard)
				wl_keyboard_destroy(display.seats[i].keyboard);

			if (display.seats[i].touch)
				wl_touch_destroy(display.seats[i].touch);

			if (display.seats[i].seat)
				wl_seat_destroy(display.seats[i].seat);
		}
	}

	if (display.wm_base)
		xdg_wm_base_destroy(display.wm_base);

	if (display.compositor)
		wl_compositor_destroy(display.compositor);

	wl_registry_destroy(display.registry);
	wl_display_flush(display.display);
	wl_display_disconnect(display.display);
}
