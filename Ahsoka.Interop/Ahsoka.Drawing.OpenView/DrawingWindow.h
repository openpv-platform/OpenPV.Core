#ifndef DRAWINGWINDOW_H_
#define DRAWINGWINDOW_H_

#ifdef  __cplusplus
extern "C" {
#endif

int InitWindowCore(char* appId, int surfaceId);
int InitWindowWithSize(bool fullScreen, int w, int h, char* appId, int surfaceId);
int InitWindowWithSurfaceId( int surfaceId, char* appId);

int UpdateSurfaceLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges);
int UpdateSurfaceSource(int surfaceId, int x, int y, int w, int h, bool commitChanges);
int UpdateSurfaceVisibility(int surfaceId, bool visibility, float opacity, bool commitChanges);

void DestoryWindowContext();

void GLSwapBuffers();
bool GetTouchData(TouchEvent *data, int timeoutInMs);
void GetWindowDimensions(int *w, int *h);
void GetSurfaceInfo(int *samples, int *stencilBits, unsigned int * gl_format);
bool UpdateSwapInterval(int interval);
void* GetFunction(const char *str);

void* WaylandDispatchThread(void *);

#ifdef __cplusplus
}
#endif
#endif