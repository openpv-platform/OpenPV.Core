#pragma once
#include <queue>
#include <mutex>
#include <condition_variable>
#include "semaphore.h"
#include "time.h"

namespace AhsokaServiceFramework
{
    class ReadyWaiter
    {
        private:
            std::mutex waitMutex;
            std::condition_variable condVar;
            bool ready = false;

        public:
            ReadyWaiter() 
            {
                
            }

            ~ReadyWaiter()
            {
                
            }

            void SetReady()
            {
                std::unique_lock<std::mutex> lock(waitMutex);
                ready = true;
                condVar.notify_one();
            }

            void WaitForReady()
            {
                std::unique_lock<std::mutex> lock(waitMutex);
                condVar.wait(lock, [this]{ return ready;});
            }
            
    };


}