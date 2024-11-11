#ifndef WAYLANDCLIENTINTEROP_H_
#define WAYLANDCLIENTINTEROP_H_
#include <stdio.h>

#include <wayland-client.h>
#include <wayland-server.h>
#include <wayland-cursor.h>
#include <wayland-client-protocol.h>
#include <wayland-egl.h>
#include <EGL/egl.h>

#include "xdg-shell-client-protocol.h"
#include "XdgTouch.h"

#ifdef ISPRO
#include <ilm/ivi-application-client-protocol.h>
#endif

#ifdef  __cplusplus
extern "C" {
#endif

#define MAX_SEATS 10
#define CAIRO_DEBUG 1

typedef struct 
{
	uint32_t seat_name;
	struct wl_seat *seat;
	struct wl_pointer *pointer;
	struct wl_touch *touch;
	struct wl_keyboard *keyboard;

} OpenPvSeat;

struct OpenPvWindow;
typedef struct {
	struct wl_display *display;
	struct wl_registry *registry;
	struct wl_compositor *compositor;
	struct xdg_wm_base *wm_base;
	struct ivi_application *ivi_app;
	
	OpenPvSeat seats[MAX_SEATS];
	
	struct wl_shm *shm;
	struct wl_cursor_theme *cursor_theme;
	struct wl_cursor *default_cursor;
	struct wl_surface *cursor_surface;
	struct wl_output *output;

	struct {
		EGLDisplay dpy;
		EGLContext ctx;
		EGLConfig conf;
	} egl;

	bool initialized;
	struct OpenPvWindow *window;
} OpenPvDisplay;

typedef struct OpenPvWindow {
	OpenPvDisplay *display;
	uint32_t benchmark_time, frames;
	struct wl_egl_window *native;
	struct wl_surface *surface;
	struct xdg_surface *xdg_surface;
	struct xdg_toplevel *xdg_toplevel;
	EGLSurface egl_surface;
	struct wl_callback *callback;
	int fullscreen, opaque, buffer_size, frame_sync, delay;
	bool wait_for_configure;
	char* appId;
	int width, height;
	bool initialized;
	int surfaceId;

} OpenPvWindow;



int InitWaylandClient(OpenPvDisplay *display, OpenPvWindow *window);

#ifdef  __cplusplus
}
#endif
#endif