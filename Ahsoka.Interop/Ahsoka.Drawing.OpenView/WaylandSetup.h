#ifndef WAYLANDSETUP_H_
#define WAYLANDSETUP_H_
#include <stdio.h>

#include <wayland-client.h>
#include <wayland-server.h>
#include <wayland-cursor.h>
#include <wayland-client-protocol.h>
#include <wayland-egl.h>
#include <EGL/egl.h>
#include "xdg-shell-client-protocol.h"
#include "XdgTouch.h"

#include "DrawingWindowContext.h"

#ifdef ISPRO
#include <ilm/ivi-application-client-protocol.h>
#endif


#ifdef  __cplusplus
extern "C" {
#endif

int InitWaylandClient(DrawingWindowContext *display);

#ifdef  __cplusplus
}
#endif
#endif