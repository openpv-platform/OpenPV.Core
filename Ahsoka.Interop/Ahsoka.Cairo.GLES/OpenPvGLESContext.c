#include <stdio.h>
#include <unistd.h>

#include "OpenPvGLESContext.h"
#include "WaylandClientInterop.h"
#include "CairoContext.h"
#include "EGLSurface.h"
#include "weston-platform.h"

static OpenPvDisplay 		display = { 0 };
static OpenPvWindow 		window  = { 0 };
static OpenPvCairoContext 	context = { 0 };

void GetWindowDimensions(int *w, int *h)
{
	if (window.fullscreen)
	{
		*w = window.toplevel_w;
		*h = window.toplevel_h;
	}
	else
	{
		*w = window.req_width;
		*h = window.req_height;
	}
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
		

	if (fullScreen)
		window.fullscreen = 1;
	window.req_width = w;
	window.req_height = h;
		
	
	ret = InitWaylandClient(&display, &window);
	if (ret != 0)
		return -1;

	ret = InitEGL(&display, &window);
	if (ret != 0)
		return -1;

	ret = CreateXdgLayer(&display, &window);
	if (ret != 0)
		return -1;

	// roundtrip to wait for any pending requests
	wl_display_roundtrip(display.display);

	CreateEglWindow(&display, &window);

	context.display = &display;
	context.window = &window;
	ret = InitCairoFW(&context);
	if (ret != 0)
		return -1;

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

cairo_surface_t* GetCairoSurface()
{
	if (!context.cairo_surface)
		return NULL;

	return context.cairo_surface;
}


cairo_t* GetCairoContext()
{
	if (!context.cr)
		return NULL;

	return context.cr;
}

OpenPvDisplay* GetOpenPvDisplay()
{
	if (!display.initialized)
		return NULL;
	
	return &display;
}


void GLSwapBuffers(cairo_surface_t *surface)
{
	cairo_gl_surface_swapbuffers(surface);
}


bool UpdateSwapInterval(int interval)
{
	return eglSwapInterval(display.egl.dpy, interval);
}



void DestoryWindowContext()
{
	// Start from the top down with Cairo
	cairo_device_destroy(context.cairo_device);
	cairo_device_finish(context.cairo_device);

	cairo_surface_destroy(context.cairo_surface);
	cairo_surface_finish(context.cairo_surface);

	eglMakeCurrent(display.egl.dpy, EGL_NO_SURFACE, EGL_NO_SURFACE,
		       EGL_NO_CONTEXT);

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
