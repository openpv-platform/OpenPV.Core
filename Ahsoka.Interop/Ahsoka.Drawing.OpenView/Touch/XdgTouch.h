#ifndef XDGTOUCH_H_
#define XDGTOUCH_H_

#include <pthread.h>
#include <sys/queue.h>
#include <stdbool.h>

#define Q_CAP 10000

#ifdef  __cplusplus
extern "C" {
#endif

enum TouchType 
{
    touch_down,
    touch_up,
    touch_continue,
    touch_frame,
    touch_cancel,
    touch_none
};

typedef struct 
{
    enum TouchType type;
    int x;
    int y;
    int id;
} TouchEvent;

struct TouchEntry 
{
    TouchEvent data;
    TAILQ_ENTRY(TouchEntry) entries;
};


void createXdgTouchQueue();

void enqueue(TouchEvent *item);
bool dequeue(TouchEvent *item, unsigned int timeoutMS);

#ifdef  __cplusplus
}
#endif
#endif

