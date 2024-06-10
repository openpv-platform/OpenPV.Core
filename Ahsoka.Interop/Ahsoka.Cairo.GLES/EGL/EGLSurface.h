#ifndef EGLSURFACE_H_
#define EGLSURFACE_H_
#include <GLES2/gl2.h>
#include <EGL/egl.h>

#include "WaylandClientInterop.h"
#ifdef  __cplusplus
extern "C" {
#endif

int InitEGL(OpenPvDisplay *display, OpenPvWindow *window);
int CreateXdgLayer(OpenPvDisplay *display, OpenPvWindow *window);
int CreateEglWindow(OpenPvDisplay *display, OpenPvWindow *window);


#ifdef  __cplusplus
}
#endif
#endif