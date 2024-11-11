#ifndef OPENPVGLESCONTEXT_H_
#define OPENPVGLESCONTEXT_H_

#include "XdgTouch.h"
#include <GLES2/gl2.h>
#include <EGL/egl.h>

#ifdef  __cplusplus
extern "C" {
#endif

int InitWindowInterop(int w, int h, bool fullScreen, char* appId, int surfaceId);
void DestoryWindowContext();

void * GetFunction(const char *str);

void GLSwapBuffers();
bool GetTouchData(TouchEvent *data, int timeoutInMs);
void GetWindowDimensions(int *w, int *h);
void GetSurfaceInfo(int *samples, int *stencilBits, unsigned int * gl_format);

bool UpdateSwapInterval(int interval);

#ifdef  __cplusplus
}
#endif
#endif