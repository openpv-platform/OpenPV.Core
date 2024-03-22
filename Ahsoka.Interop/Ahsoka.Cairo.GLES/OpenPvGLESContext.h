#ifndef OPENPVGLESCONTEXT_H_
#define OPENPVGLESCONTEXT_H_

#include <cairo/cairo.h>
#include <cairo/cairo-gl.h>
#include "XdgTouch.h"

#ifdef  __cplusplus
extern "C" {
#endif

int InitWindowInterop(int x, int y, int w, int h, bool fullScreen);
void DestoryWindowContext();

cairo_surface_t *GetCairoSurface();
cairo_t 		*GetCairoContext();

void GLSwapBuffers(cairo_surface_t *surface);
bool GetTouchData(TouchEvent *data, int timeoutInMs);
void GetWindowDimensions(int *w, int *h);
bool UpdateSwapInterval(int interval);

#ifdef  __cplusplus
}
#endif
#endif