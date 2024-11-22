#ifndef WAYLANDSETUP_H_
#define WAYLANDSETUP_H_
#include <stdio.h>

#include "DrawingWindowContext.h"

#ifdef  __cplusplus
extern "C" {
#endif

int InitWaylandClient(DrawingWindowContext *display);

#ifdef  __cplusplus
}
#endif
#endif