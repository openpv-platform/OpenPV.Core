#ifndef DRAWINGWINDOW_H_
#define DRAWINGWINDOW_H_

#include "XdgTouch.h"
#include <GLES2/gl2.h>
#include <EGL/egl.h>

#ifdef  __cplusplus
extern "C" {
#endif

int InitWindowCore(char* appId, int surfaceId);
int InitWindowWithSize(bool fullScreen,int x, int y, int w, int h, char* appId, int surfaceId);
int InitWindowWithSurfaceId( int surfaceId, char* appId);
void DestoryWindowContext();

void GLSwapBuffers();
bool GetTouchData(TouchEvent *data, int timeoutInMs);
void GetWindowDimensions(int *w, int *h);
void GetSurfaceInfo(int *samples, int *stencilBits, unsigned int * gl_format);

bool UpdateSwapInterval(int interval);

void* GetFunction(const char *str);

#ifdef __cplusplus
}
#endif
#endif