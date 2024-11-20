#ifndef DRAWINGWINDOWCONTEXT_H_
#define DRAWINGWINDOWCONTEXT_H_
#include <stdio.h>


#define MAX_SEATS 10
#define CAIRO_DEBUG 1

typedef struct 
{
	uint32_t seat_name;
	struct wl_seat *seat;
	struct wl_pointer *pointer;
	struct wl_touch *touch;
	struct wl_keyboard *keyboard;

} WaylandSeatInfo;


struct OpenPvWindow;
typedef struct 
{
	// Wayland Setup
	struct wl_display *display;
	struct wl_registry *registry;
	struct wl_compositor *compositor;
	struct wl_shm *shm;
	struct wl_cursor_theme *cursor_theme;
	struct wl_cursor *default_cursor;
	struct wl_surface *cursor_surface;
	struct wl_output *output;
	struct wl_egl_window *native;
	struct wl_surface *surface;
	struct wl_callback *callback;
	
	WaylandSeatInfo seats[MAX_SEATS];

	//XDG Setup
	struct xdg_wm_base *wm_base;
	struct xdg_surface *xdg_surface;
	struct xdg_toplevel *xdg_toplevel;

	//IVI App Setup - Pro Only
	struct ivi_application *ivi_app;
	
	// GLES Info
	EGLSurface egl_surface;
	struct 
	{
		EGLDisplay dpy;
		EGLContext ctx;
		EGLConfig conf;
	} egl;

	bool initialized;

	uint32_t benchmark_time, frames;
	int fullscreen, opaque, buffer_size, frame_sync, delay;
	bool wait_for_configure;
	char* appId;
	int width, height;
	int surfaceId;
	int surface_width, surface_height;
	
} DrawingWindowContext;

#endif