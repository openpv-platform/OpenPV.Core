#include <stdlib.h>
#include "GLSetup.h"
#include "weston-platform.h"

int PrintConfigs(EGLDisplay egl_disp, EGLConfig* egl_configs, EGLint egl_num_configs);

int InitEGL(DrawingWindowContext *display)
{
    EGLint major, minor, count, n, size, i;
	EGLBoolean ret;

	static const EGLint context_attribs[] = {
		EGL_CONTEXT_CLIENT_VERSION, 2,
		EGL_NONE
	};
	const char *extensions;

	EGLint config_attribs[] = {
		EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
		EGL_RED_SIZE, 8,
		EGL_GREEN_SIZE, 8,
		EGL_BLUE_SIZE, 8,
		EGL_ALPHA_SIZE, 8,
		EGL_STENCIL_SIZE, 8,
		EGL_SAMPLES, 0,
		EGL_DEPTH_SIZE, 24,
		EGL_RENDERABLE_TYPE, EGL_OPENGL_ES2_BIT,
		EGL_NONE
	};

	// Clear Alpha
	if (display->opaque || display->buffer_size == 16)
		config_attribs[9] = 0;

 	display->egl.dpy = weston_platform_get_egl_display(EGL_PLATFORM_WAYLAND_KHR, display->display, NULL);
    if (display->egl.dpy == EGL_NO_DISPLAY)
        return -1;
    
    if (CAIRO_DEBUG) 
		printf("-- EGL Display Successfully connected\r\n");

    if (eglInitialize(display->egl.dpy, &major, &minor) != EGL_TRUE)
        return -1;
    
    if (CAIRO_DEBUG) 
		printf("-- Succesfully initlized EGL Display -> EGL major: %d, minor %d\r\n", major, minor);

    if (!eglBindAPI(EGL_OPENGL_ES_API))
        return -1;
    
    if (CAIRO_DEBUG) 
		printf("-- EGL API BIND Success...\r\n");

	if (!eglGetConfigs(display->egl.dpy, NULL, 0, &count) || count < 1)
		return -1;

	 EGLConfig* configs = new EGLConfig[count];

    int rc = eglGetConfigs(display->egl.dpy, configs, count, &count);
    if (rc != EGL_TRUE) {
        printf("eglGetConfigs");
		return -1;
    }

	// Print out EGL Config.
	//PrintConfigs(display->egl.dpy,configs,count);

	ret = eglChooseConfig(display->egl.dpy, config_attribs, configs, count, &n);

	for (i = 0; i < n; i++) 
	{
	
		EGLint config_id;
		eglGetConfigAttrib(display->egl.dpy, configs[i], EGL_CONFIG_ID, &config_id);
		
		if (CAIRO_DEBUG) 
			printf("-- SELECTED CONFIG %d %d...\r\n",config_id, i);

		display->egl.conf = configs[i];

		break;
	}

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
#ifndef ISPRO
	DrawingWindowContext *display = (DrawingWindowContext *)data;
	display->width = width;
	display->height = height;
#endif
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
	DrawingWindowContext *display = (DrawingWindowContext *)data;

	xdg_surface_ack_configure(surface, serial);

	// critical for our surface to send acknowledge above. Error if not sent
	display->wait_for_configure = false;
}

static const struct xdg_surface_listener xdg_surface_listener = 
{
	handle_surface_configure
};

int CreateXdgLayer(DrawingWindowContext *display)
{
    display->surface = wl_compositor_create_surface(display->compositor);

    display->xdg_surface = xdg_wm_base_get_xdg_surface(display->wm_base, display->surface);
    xdg_surface_add_listener(display->xdg_surface, &xdg_surface_listener, display);

    display->xdg_toplevel = xdg_surface_get_toplevel(display->xdg_surface);
    xdg_toplevel_add_listener(display->xdg_toplevel, &xdg_toplevel_listener, display);

    xdg_toplevel_set_title(display->xdg_toplevel, display->appId);
    xdg_toplevel_set_app_id(display->xdg_toplevel, display->appId);
  
    if (display->fullscreen)
        xdg_toplevel_set_fullscreen(display->xdg_toplevel, NULL);

	// roundtrip to wait for any pending requests
	wl_display_roundtrip(display->display);

	return 0;
}

int CreateEglWindow(DrawingWindowContext *display)
{
	display->native = wl_egl_window_create(display->surface, display->width, display->height);

	display->egl_surface = weston_platform_create_egl_surface(display->egl.dpy, display->egl.conf, display->native, NULL);
	wl_surface_commit(display->surface);
	
     // Keep Front Buffer for Dirty Rectangles
    eglSurfaceAttrib(display->egl.dpy, display->egl_surface, EGL_SWAP_BEHAVIOR, EGL_BUFFER_PRESERVED);
  
    // Prepare for Drawing (we only use one surface currently)
    eglMakeCurrent(display->egl.dpy, display->egl_surface, display->egl_surface, display->egl.ctx);
	eglSwapInterval(display->egl.dpy, 1);

	return 0;
}

int PrintConfigs(EGLDisplay egl_disp, EGLConfig* egl_configs, EGLint egl_num_configs)
{
    EGLint val;
    int rval = EXIT_FAILURE;
    int i, rc;

    printf("EGL_VENDOR = %s\n", eglQueryString(egl_disp, EGL_VENDOR));
    printf("EGL_VERSION = %s\n", eglQueryString(egl_disp, EGL_VERSION));
    printf("EGL_CLIENT_APIS = %s\n", eglQueryString(egl_disp, EGL_CLIENT_APIS));
    printf("EGL_EXTENSIONS = %s\n\n", eglQueryString(egl_disp, EGL_EXTENSIONS));

    puts("+-----------+--------+-----+-------+------+-----------+-------+-------+---------+------+---------+--------+---------+--------+----+-----+-----+-------+-------+----+----+--------+-------+-----+-------+------+");
    puts("|           |        |     |       |      |           |       |       |         |      |         |        |         |        |    |     |     |       |       |    |    |        |       |    transparent     |");
    puts("| config id | caveat | red | green | blue | luminance | alpha | depth | stencil | mask | samples | window | pbuffer | pixmap | sw | lin | pre | gles1 | gles2 | vg | gl | native | level | red | green | blue |");
    puts("+-----------+--------+-----+-------+------+-----------+-------+-------+---------+------+---------+--------+---------+--------+----+-----+-----+-------+-------+----+----+--------+-------+-----+-------+------+");

    for (i = 0; i < egl_num_configs; i++) {
        val = -1;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_CONFIG_ID, &val);
        printf("| %9d ", val);

        val = EGL_NONE;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_CONFIG_CAVEAT, &val);
        if (val == EGL_SLOW_CONFIG) {
            printf("|  slow  ");
        } else {
            printf("|  none  ");
        }

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_RED_SIZE, &val);
        printf("| %3d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_GREEN_SIZE, &val);
        printf("| %5d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_BLUE_SIZE, &val);
        printf("| %4d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_LUMINANCE_SIZE, &val);
        printf("| %9d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_ALPHA_SIZE, &val);
        printf("| %5d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_DEPTH_SIZE, &val);
        printf("| %5d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_STENCIL_SIZE, &val);
        printf("| %7d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_ALPHA_MASK_SIZE, &val);
        printf("| %4d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_SAMPLES, &val);
        printf("| %7d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_SURFACE_TYPE, &val);
        if (val & EGL_WINDOW_BIT) {
            printf("|    x   ");
        } else {
            printf("|        ");
        }
        if (val & EGL_PBUFFER_BIT) {
            printf("|    x    ");
        } else {
            printf("|         ");
        }
        if (val & EGL_PIXMAP_BIT) {
            printf("|    x   ");
        } else {
            printf("|        ");
        }
        if (val & EGL_OPTIMAL_FORMAT_BIT_KHR) {
            printf("|  o ");
        } else if (val & EGL_LOCK_SURFACE_BIT_KHR) {
            printf("|  x ");
        } else {
            printf("|    ");
        }
        if (val & EGL_VG_COLORSPACE_LINEAR_BIT) {
            printf("|  x  ");
        } else {
            printf("|     ");
        }
        if (val & EGL_VG_ALPHA_FORMAT_PRE_BIT) {
            printf("|  x  ");
        } else {
            printf("|     ");
        }

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_RENDERABLE_TYPE, &val);
        if (val & EGL_OPENGL_ES_BIT) {
            printf("|   x   ");
        } else {
            printf("|       ");
        }
        if (val & EGL_OPENGL_ES2_BIT) {
            printf("|   x   ");
        } else {
            printf("|       ");
        }
        if (val & EGL_OPENVG_BIT) {
            printf("|  x ");
        } else {
            printf("|    ");
        }
        if (val & EGL_OPENGL_BIT) {
            printf("|  x ");
        } else {
            printf("|    ");
        }

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_NATIVE_RENDERABLE, &val);
        if (val == EGL_TRUE) {
            printf("|    x   ");
        } else {
            printf("|        ");
        }

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_LEVEL, &val);
        printf("| %5d ", val);

        val = 0;
        eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_TRANSPARENT_TYPE, &val);
        if (val == EGL_TRANSPARENT_RGB) {
            eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_TRANSPARENT_RED_VALUE, &val);
            printf("| %3d ", val);
            eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_TRANSPARENT_GREEN_VALUE, &val);
            printf("|  %3d  ", val);
            eglGetConfigAttrib(egl_disp, egl_configs[i], EGL_TRANSPARENT_BLUE_VALUE, &val);
            printf("| %3d  |\n", val);
        } else {
            printf("| n/a |  n/a  | n/a  |\n");
        }
    }

    puts("+-----------+--------+-----+-------+------+-----------+-------+-------+---------+------+---------+--------+---------+--------+----+-----+-----+-------+-------+----+----+--------+-------+-----+-------+------+");

    rval = EXIT_SUCCESS;

    return rval;
}
