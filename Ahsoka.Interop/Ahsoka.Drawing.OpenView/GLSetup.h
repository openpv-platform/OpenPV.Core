#ifndef GLSETUP_H_
#define GLSETUP_H_

#include <GLES2/gl2.h>
#include <EGL/egl.h>
#include "WaylandSetup.h"


#ifdef  __cplusplus
extern "C" {
#endif

int InitEGL(DrawingWindowContext *display);
int CreateXdgLayer(DrawingWindowContext *display);
int CreateEglWindow(DrawingWindowContext *display);

#ifdef  __cplusplus
}
#endif
#endif