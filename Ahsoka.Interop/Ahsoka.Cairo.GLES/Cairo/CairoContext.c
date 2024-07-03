#include <cairo/cairo.h>
#include <cairo/cairo-gl.h>
#include "CairoContext.h"

int InitCairoFW(OpenPvCairoContext *context)
{
    context->cairo_device = cairo_egl_device_create(context->display->egl.dpy, context->display->egl.ctx);
    if ( cairo_device_status(context->cairo_device) != CAIRO_STATUS_SUCCESS )
        return -1;

    if (context->window->fullscreen)
    {
        context->cairo_surface = cairo_gl_surface_create_for_egl(context->cairo_device,
                                                            context->window->egl_surface,
                                                            context->window->toplevel_w,
                                                            context->window->toplevel_h);
    }
    else
    {
        context->cairo_surface = cairo_gl_surface_create_for_egl(context->cairo_device,
                                                            context->window->egl_surface,
                                                            context->window->req_width,
                                                            context->window->req_height);
    }

    if (context->cairo_surface == NULL)
        return -1;

    if (CAIRO_DEBUG) printf("-- Cairo Surface Creation: Success\r\n");
    context->cr = cairo_create(context->cairo_surface);
    if (cairo_status(context->cr) != CAIRO_STATUS_SUCCESS)
        return -1;

    return 0;
}
