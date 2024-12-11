#include "WaylandSetup.h"

#include <EGL/eglext.h>
#include <limits.h>

#include "weston-egl-ext.h"
#include "weston-platform.h"
#include "wayland-util.h"

#define BTN_RIGHT		0x111
#define BTN_LEFT		0x110

static bool mouse_down = 0;

static int touch_xi = 0;
static int touch_yi = 0;

static int mouse_xi = 0;
static int mouse_yi = 0;

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

	if (CAIRO_DEBUG > 1)  
		printf("Touch Down: X[%i] Y[%i] ID[%i]\r\n", xi, yi, id);
	
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
	if (CAIRO_DEBUG > 1)  
		printf("Touch UP: X[%i] Y[%i] ID[%i]\r\n", touch_xi, touch_yi, id);

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

static void pointer_handle_motion(void *data, struct wl_pointer *wl_pointer, uint32_t time,
               wl_fixed_t surface_x, wl_fixed_t surface_y)
{
	mouse_xi = wl_fixed_to_int(surface_x);
	mouse_yi = wl_fixed_to_int(surface_y);

	if (mouse_down)
	{
		TouchEvent t = {
			.type = touch_continue,
			.x  = mouse_xi,
			.y =  mouse_yi,
			.id = INT_MAX
		};
		enqueue(&t);
			
		if (CAIRO_DEBUG) printf("MOUSE MOVED: X[%i] Y[%i] \r\n", mouse_xi, mouse_yi);
	}
}

static void pointer_handle_button(void *data, struct wl_pointer *pointer,
		uint32_t serial, uint32_t time, uint32_t button, uint32_t state) 
{
	struct wl_seat *seat = (struct wl_seat*)data;
	
	// If the user presses the left pointer button, start an interactive move
	// of the toplevel
	if (button == BTN_LEFT && state == WL_POINTER_BUTTON_STATE_PRESSED) 
	{
		if (CAIRO_DEBUG) printf("-- WL_POINTER_BUTTON_STATE_PRESSED X[%i] Y[%i] \r\n", mouse_xi, mouse_yi);

		mouse_down = true;

		TouchEvent t = {
			.type = touch_down,
			.x  = mouse_xi,
			.y =  mouse_yi,
			.id = INT_MAX
		};
		enqueue(&t);
	}
	if (button == BTN_LEFT && state == WL_POINTER_BUTTON_STATE_RELEASED) 
	{
		if (CAIRO_DEBUG) printf("-- WL_POINTER_BUTTON_STATE_RELEASED X[%i] Y[%i] \r\n", mouse_xi, mouse_yi);
	
		mouse_down = false;

		TouchEvent t = {
			.type = touch_up,
			.x  = mouse_xi,
			.y =  mouse_yi,
			.id = INT_MAX
		};
		enqueue(&t);
	}
}

static void pointer_handle_enter(void*, struct wl_pointer*, uint32_t, struct wl_surface*, wl_fixed_t, wl_fixed_t)
{

}

static void pointer_handle_leave(void*, struct wl_pointer*, unsigned int, struct wl_surface*)
{

}


static void pointer_handle_axis(void *data, struct wl_pointer *wl_pointer, uint32_t time, 
			uint32_t axis, wl_fixed_t value)
{

}

static const struct wl_pointer_listener pointer_listener = 
{
	.enter = pointer_handle_enter,
	.leave = pointer_handle_leave,
	.motion = pointer_handle_motion,
	.button = pointer_handle_button,
	.axis = pointer_handle_axis,
};


static int get_seat(DrawingWindowContext *d, struct wl_seat *seat)
{
	// Evaluate Seats and Available Seats
	int seatIndex = -1;
	for (size_t i = 0; i < MAX_SEATS; i++)
		if (d->seats[i].seat == seat)
			seatIndex = i;

	return seatIndex;
}

static void seat_handle_capabilities(void *data, struct wl_seat *seat,
			 unsigned int caps)
{
	if (CAIRO_DEBUG) printf("-- Handling Seat Capabilities   SEAT:[%i] CAP:[%i] \r\n", seat, caps);

	DrawingWindowContext *d = (DrawingWindowContext *) data;
	
	int seatIndex = get_seat(d, seat);
	if (seatIndex < 0)
		return;
	
	if ((caps & WL_SEAT_CAPABILITY_TOUCH) && !d->seats[seatIndex].touch)
	{
		if (CAIRO_DEBUG) printf("-- CREATE TOUCH\r\n");
		d->seats[seatIndex].touch = wl_seat_get_touch(seat);
		wl_touch_set_user_data(d->seats[seatIndex].touch, d);
		wl_touch_add_listener(d->seats[seatIndex].touch, &touch_listener, d);
	}
	else if (!(caps & WL_SEAT_CAPABILITY_TOUCH) && d->seats[seatIndex].touch)
	{
		if (CAIRO_DEBUG) printf("-- REMOVE TOUCH\r\n");
		wl_touch_destroy(d->seats[seatIndex].touch);
		d->seats[seatIndex].touch = NULL;
	}
	else if (caps & WL_SEAT_CAPABILITY_POINTER) 
	{
	 	if (CAIRO_DEBUG) printf("-- ADDING POINTER\r\n");
		struct wl_pointer *pointer = wl_seat_get_pointer(seat);
		wl_pointer_add_listener(pointer, &pointer_listener, seat);
	}
}

static const struct wl_seat_listener seat_listener = 
{
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
	DrawingWindowContext *d = (DrawingWindowContext *) data;
	
	if (CAIRO_DEBUG > 1) 
		printf("-- %s [%i]\r\n", interface, name );
	
	if (strcmp(interface, "wl_compositor") == 0)
		d->compositor = (struct wl_compositor*)wl_registry_bind(registry, name, (&wl_compositor_interface), 1);
	else if (strcmp(interface, "xdg_wm_base") == 0)
	{
		d->wm_base = (struct xdg_wm_base*)wl_registry_bind(registry, name, &xdg_wm_base_interface, 1);
		xdg_wm_base_add_listener(d->wm_base, &wm_base_listener, d);
	}

    else if (strcmp(interface, "wl_seat") == 0)
    {
		// Find a Seat Slot
		int availableSeat = -1;
		for (size_t i = 0; i < MAX_SEATS; i++)
			if (d->seats[i].seat == 0)
				availableSeat = i;
		
		// If Available Add our Seat
		if (availableSeat >=0)
		{
			d->seats[availableSeat].seat_name = name;
			d->seats[availableSeat].seat = (struct wl_seat*)wl_registry_bind(registry, name, &wl_seat_interface, 1);
			wl_seat_add_listener(d->seats[availableSeat].seat, &seat_listener, d);

			if (CAIRO_DEBUG) printf("-- ADDING SEAT[%i] @ [%i] and [%i]\r\n", d->seats[availableSeat].seat, availableSeat, name );
		}
		else
		{
			printf("-- NO SEAT SLOTS AVAILABLE\r\n");
		}
	}
    else if (strcmp(interface, "wl_shm") == 0)
	    d->shm = (struct wl_shm*)wl_registry_bind(registry, name, &wl_shm_interface, 1);
	
	// Handle Platform Specific Callbacks
	if(d->registryCallback != 0)
		d->registryCallback(data, registry, name,  interface, version);

}

static void registry_handle_global_remove(void *data, struct wl_registry *registry,
			      uint32_t name)
{
	DrawingWindowContext *d = (DrawingWindowContext *) data;
	
	for (size_t i = 0; i < MAX_SEATS; i++)
		if (d->seats[i].seat_name == name)
		{
			if (CAIRO_DEBUG) printf("-- REMOVING SEAT [%i]\r\n",name);
			
			if (d->seats[i].pointer)
				wl_pointer_destroy(d->seats[i].pointer);

			if (d->seats[i].keyboard)
				wl_keyboard_destroy(d->seats[i].keyboard);

			if (d->seats[i].touch)
				wl_touch_destroy(d->seats[i].touch);

			if (d->seats[i].seat)
				wl_seat_destroy(d->seats[i].seat);
	
			d->seats[i].pointer = 0;
			d->seats[i].keyboard = 0;
			d->seats[i].touch = 0;
			d->seats[i].seat = 0;
			d->seats[i].seat_name = -1;

			if (CAIRO_DEBUG) printf("-- SEAT REMOVED\r\n");
		}
}

static const struct wl_registry_listener registry_listener = {
	registry_handle_global,
	registry_handle_global_remove
};

int InitWaylandClient(DrawingWindowContext *display)
{
	display->display = wl_display_connect(NULL);
	display->registry = wl_display_get_registry(display->display);
	wl_registry_add_listener(display->registry, &registry_listener, display);
	wl_display_roundtrip(display->display);
    return 0;
}
