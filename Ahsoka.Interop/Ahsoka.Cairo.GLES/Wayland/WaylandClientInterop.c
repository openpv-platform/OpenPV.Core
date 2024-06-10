#include "WaylandClientInterop.h"

#include <EGL/eglext.h>

#include "weston-egl-ext.h"
#include "weston-platform.h"
#include "wayland-util.h"


static int touch_xi = 0;
static int touch_yi = 0;

static void touch_handle_down(void *data, struct wl_touch *wl_touch,
		  uint32_t serial, uint32_t time, struct wl_surface *surface,
		  int32_t id, wl_fixed_t x_w, wl_fixed_t y_w)
{
	int xi = wl_fixed_to_int(x_w);
	int yi = wl_fixed_to_int(y_w);
	TouchEvent t = {
		.type = touch_down,
		.x  = xi,
		.y =  yi,
		.id = id
	};

	touch_xi = xi;
	touch_yi = yi;

	// printf("Touch Down: X[%i] Y[%i] ID[%i]\n\r", xi, yi, id);
	enqueue(&t);
	
	// popup windows that can be dragged and dropped need to use xdg move below
	// xdg_toplevel_move(w->xdg_toplevel, d->seat, serial);
}

static void touch_handle_up(void *data, struct wl_touch *wl_touch,
		uint32_t serial, uint32_t time, int32_t id)
{
	TouchEvent t = {
		.type = touch_up,
		.x  = touch_xi,
		.y =  touch_yi,
		.id = id
	};

	enqueue(&t);
	// printf("Touch UP: X[%i] Y[%i] ID[%i]\n\r", touch_xi, touch_yi, id);

	// reset touch_xi & yi values to make sure we don't 'double' touch
	touch_xi = 0;
	touch_yi = 0;
}

static void touch_handle_motion(void *data, struct wl_touch *wl_touch,
		    uint32_t time, int32_t id, wl_fixed_t x_w, wl_fixed_t y_w)
{
	int xi = wl_fixed_to_int(x_w);
	int yi = wl_fixed_to_int(y_w);
	TouchEvent t = {
		.type = touch_continue,
		.x  = xi,
		.y =  yi,
		.id = id
	};
	enqueue(&t);
}

static void touch_handle_frame(void *data, struct wl_touch *wl_touch)
{
	// Nothing to do here
}

static void touch_handle_cancel(void *data, struct wl_touch *wl_touch)
{
	// Nothing to do here
}

static const struct wl_touch_listener touch_listener = {
	touch_handle_down,
	touch_handle_up,
	touch_handle_motion,
	touch_handle_frame,
	touch_handle_cancel,
};



static void seat_handle_capabilities(void *data, struct wl_seat *seat,
			 enum wl_seat_capability caps)
{
	OpenPvDisplay *d = (OpenPvDisplay *) data;

	if ((caps & WL_SEAT_CAPABILITY_TOUCH) && !d->touch)
	{
		d->touch = wl_seat_get_touch(seat);
		wl_touch_set_user_data(d->touch, d);
		wl_touch_add_listener(d->touch, &touch_listener, d);
	}
	else if (!(caps & WL_SEAT_CAPABILITY_TOUCH) && d->touch)
	{
		wl_touch_destroy(d->touch);
		d->touch = NULL;
	}
}

static const struct wl_seat_listener seat_listener = {
	seat_handle_capabilities,
};

static void xdg_wm_base_ping(void *data, struct xdg_wm_base *shell, uint32_t serial)
{
	xdg_wm_base_pong(shell, serial);
}

static const struct xdg_wm_base_listener wm_base_listener = {
	xdg_wm_base_ping,
};

static void registry_handle_global(void *data, struct wl_registry *registry,
		       uint32_t name, const char *interface, uint32_t version)
{
	OpenPvDisplay *d = (OpenPvDisplay *) data;
	// const struct wl_interface *interface -> static_cast<const wl_interface *>
	if (strcmp(interface, "wl_compositor") == 0)
		d->compositor = wl_registry_bind(registry, name, (&wl_compositor_interface), 1);
	
	else if (strcmp(interface, "xdg_wm_base") == 0)
	{
		d->wm_base = wl_registry_bind(registry, name, &xdg_wm_base_interface, 1);
		xdg_wm_base_add_listener(d->wm_base, &wm_base_listener, d);
	}
    else if (strcmp(interface, "wl_seat") == 0)
    {
		d->seat = wl_registry_bind(registry, name, &wl_seat_interface, 1);
		wl_seat_add_listener(d->seat, &seat_listener, d);
	}
    else if (strcmp(interface, "wl_shm") == 0)
	    d->shm = wl_registry_bind(registry, name, &wl_shm_interface, 1);
}

static void registry_handle_global_remove(void *data, struct wl_registry *registry,
			      uint32_t name)
{
}

static const struct wl_registry_listener registry_listener = {
	registry_handle_global,
	registry_handle_global_remove
};



int InitWaylandClient(OpenPvDisplay *display, OpenPvWindow *window)
{
	display->display = wl_display_connect(NULL);
	display->registry = wl_display_get_registry(display->display);
	wl_registry_add_listener(display->registry, &registry_listener, display);
	wl_display_roundtrip(display->display);
    return 0;
}
