#pragma once
#include <queue>
#include <mutex>
#include <condition_variable>
#include "semaphore.h"
#include "time.h"

namespace AhsokaServiceFramework
{
    class Semaphore
    {
        private:
            sem_t semaphore;
            bool didInitialize;

        public:
            Semaphore() : didInitialize(false)
            {
                int result = sem_init(&semaphore, 0, 0);

                if (result == 0)
                    didInitialize = true;
                else
                {
                    PrintLastError(__FILE__, __LINE__);
                }
            }

            ~Semaphore()
            {
                if (didInitialize)
                    sem_destroy(&semaphore);
            }

            bool Signal()
            {
                if (didInitialize)
                {
                    return (sem_post(&semaphore) == 0 ? true : false);
                }
                else
                {
                    PrintDidNotInitializeError(__FILE__, __LINE__);
                    return false;
                }
            }

            bool Wait()
            {
                if (didInitialize)
                {
                    return (sem_wait(&semaphore) == 0 ? true : false);
                }
                else
                {
                    PrintDidNotInitializeError(__FILE__, __LINE__);
                    return false;
                }
            }

            bool WaitFor(int timeoutInMs)
            {
                // double check using realtime clock
                if (didInitialize)
                {
                    struct timespec tm;
                    clock_gettime(CLOCK_REALTIME, &tm);

                    long nsecTobeadded = (timeoutInMs % 1000) * 1000000L + tm.tv_nsec;
                    tm.tv_nsec = nsecTobeadded % 1000000000L;
                    tm.tv_sec += (nsecTobeadded / 1000000000L) + (timeoutInMs / 1000);
                    return sem_timedwait(&semaphore, &tm) == 0 ? true : false;
                }
                else
                {
                    PrintDidNotInitializeError(__FILE__, __LINE__);
                    return false;
                }
            }

            void PrintLastError(const char *file, int line)
            {
                //printf(_SLOG_SETCODE(_SLOGC_TEST, 0), _SLOG_ERROR, "PID %d, TID %d: Semaphore ERROR(%d): %s | %s (%d)", getpid(), gettid(), errno, strerror(errno), file, line);
            }

        private:
            void PrintDidNotInitializeError(const char *file, int line)
            {
                //printf(_SLOG_SETCODE(_SLOGC_TEST, 0), _SLOG_ERROR, "PID %d, TID %d: Semaphore did NOT initialize | %s (%d)", getpid(), gettid(), file, line);
            }
    };


    template<typename T>
    class BlockingQueue
    {
        private:
            std::queue<T> workQueue;
            std::mutex queueMutex;
            AhsokaServiceFramework::Semaphore workSem;

        public:
            T Pop()
            {
                // Wait for Work.
                if (workSem.Wait())
                {
                    // Fetch work item.
                    queueMutex.lock();
                    T item = workQueue.front();
                    workQueue.pop();
                    queueMutex.unlock();
                    return item;
                }

                workSem.PrintLastError(__FILE__, __LINE__);
                return T();
            }

            bool PopWithTimeOut(int timeoutInMs, T& item)
            {
                // Wait for Work.
                if (workSem.WaitFor(timeoutInMs))
                {
                    //Fetch work item.
                    queueMutex.lock();
                    item = workQueue.front();
                    workQueue.pop();
                    queueMutex.unlock();

                    return true;
                }
                else
                    return false;
            }

            void Push(const T& item)
            {

                queueMutex.lock();
                workQueue.push(item);
                queueMutex.unlock();

                // Signal working to begin.
                workSem.Signal();
            }

            bool Empty()
            {
                bool isEmpty = true;
                queueMutex.lock();
                isEmpty = workQueue.empty();
                queueMutex.unlock();
                return isEmpty;
            }

            size_t Size()
            {
                size_t size = 0;
                queueMutex.lock();
                size = workQueue.size();
                queueMutex.unlock();
                return size;
            }
    };
}