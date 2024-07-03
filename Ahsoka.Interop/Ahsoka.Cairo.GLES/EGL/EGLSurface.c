#include <stdlib.h>
#include "EGLSurface.h"
#include "weston-platform.h"


int InitEGL(OpenPvDisplay *display, OpenPvWindow *window)
{
    EGLint major, minor, count, n, size, i;
	EGLConfig *configs;
	EGLBoolean ret;

	static const EGLint context_attribs[] = {
		EGL_CONTEXT_CLIENT_VERSION, 2,
		EGL_NONE
	};
	const char *extensions;

	EGLint config_attribs[] = {
		EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
		EGL_RED_SIZE, 1,
		EGL_GREEN_SIZE, 1,
		EGL_BLUE_SIZE, 1,
		EGL_ALPHA_SIZE, 1,
		EGL_RENDERABLE_TYPE, EGL_OPENGL_ES2_BIT,
		EGL_NONE
	};

	

	if (window->opaque || window->buffer_size == 16)
		config_attribs[9] = 0;

 	display->egl.dpy = weston_platform_get_egl_display(EGL_PLATFORM_WAYLAND_KHR, display->display, NULL);
    if (display->egl.dpy == EGL_NO_DISPLAY)
        return -1;
    
    if (CAIRO_DEBUG) printf("-- EGL Display Successfully connected\r\n");

    if (eglInitialize(display->egl.dpy, &major, &minor) != EGL_TRUE)
        return -1;
    
    if (CAIRO_DEBUG) printf("-- Succesfully initlized EGL Display -> EGL major: %d, minor %d\r\n", major, minor);

    if (!eglBindAPI(EGL_OPENGL_ES_API))
        return -1;
    
    if (CAIRO_DEBUG) printf("-- EGL API BIND Success...\r\n");
    if (!eglGetConfigs(display->egl.dpy, NULL, 0, &count) || count < 1)
		return -1;

	configs = calloc(count, sizeof *configs);
	// assert(configs);

	ret = eglChooseConfig(display->egl.dpy, config_attribs,
			      configs, count, &n);
	// assert(ret && n >= 1);

	for (i = 0; i < n; i++) {
		EGLint buffer_size, red_size;
		eglGetConfigAttrib(display->egl.dpy, configs[i], EGL_BUFFER_SIZE, &buffer_size);
		eglGetConfigAttrib(display->egl.dpy, configs[i], EGL_RED_SIZE, &red_size);
		if ((window->buffer_size == 0 || window->buffer_size == buffer_size) && red_size < 10)
		{
			display->egl.conf = configs[i];
			break;
		}
	}
	free(configs);

	display->egl.ctx = eglCreateContext(display->egl.dpy, display->egl.conf, EGL_NO_CONTEXT, context_attribs);
	
	if (display->egl.ctx == NULL)
        return -1;
    
    if (CAIRO_DEBUG)  printf("-- EGL context creation success...\r\n");
    return 0;
}








static void handle_toplevel_configure(void *data, struct xdg_toplevel *toplevel,
			  int32_t width, int32_t height,
			  struct wl_array *states)
{
	OpenPvWindow *window = (OpenPvWindow *)data;
	window->toplevel_w = width;
	window->toplevel_h = height;
}

static void handle_toplevel_close(void *data, struct xdg_toplevel *xdg_toplevel)
{
}

static const struct xdg_toplevel_listener xdg_toplevel_listener = {
	handle_toplevel_configure,
	handle_toplevel_close,
};




static void handle_surface_configure(void *data, struct xdg_surface *surface,
			 uint32_t serial)
{
	OpenPvWindow *window = data;

	xdg_surface_ack_configure(surface, serial);

	// critical for our surface to send acknowledge above. Error if not sent
	window->wait_for_configure = false;
}

static const struct xdg_surface_listener xdg_surface_listener = {
	handle_surface_configure
};

int CreateXdgLayer(OpenPvDisplay *display, OpenPvWindow *window)
{
	window->surface = wl_compositor_create_surface(display->compositor);
	window->xdg_surface = xdg_wm_base_get_xdg_surface(display->wm_base, window->surface);
	xdg_surface_add_listener(window->xdg_surface, &xdg_surface_listener, window);

	window->xdg_toplevel = xdg_surface_get_toplevel(window->xdg_surface);
	xdg_toplevel_add_listener(window->xdg_toplevel, &xdg_toplevel_listener, window);

	xdg_toplevel_set_title(window->xdg_toplevel, "Ahsoka-EGL");
	xdg_toplevel_set_app_id(window->xdg_toplevel, "org.Ahsoka.EGL");

	window->wait_for_configure = true;
	if (window->fullscreen)
		xdg_toplevel_set_fullscreen(window->xdg_toplevel, NULL);

	return 0;
}

int CreateEglWindow(OpenPvDisplay *display, OpenPvWindow *window)
{
	if (window->fullscreen)
		window->native = wl_egl_window_create(window->surface, window->toplevel_w, window->toplevel_h);
	else
		window->native = wl_egl_window_create(window->surface, window->req_width, window->req_height);

	window->egl_surface = weston_platform_create_egl_surface(display->egl.dpy, display->egl.conf, window->native, NULL);
	wl_surface_commit(window->surface);
	eglMakeCurrent(window->display->egl.dpy, window->egl_surface, window->egl_surface, window->display->egl.ctx);
	eglSwapInterval(display->egl.dpy, 1);
	return 0;
}



