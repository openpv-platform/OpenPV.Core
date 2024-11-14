#pragma once
#include <vector>
#include <string>
#include <mutex>
#include <google/protobuf/message.h>
#include "Configuration.pb.h"

namespace AhsokaServiceFramework
{
    class AhsokaRuntime;

    class IAhsokaEndPoint
    {
        public:

            virtual AhsokaSystem::ServiceConfiguration& GetRuntimeConfiguration() = 0;

            virtual void SetRuntime(AhsokaRuntime*) = 0;
            
            virtual void Connect() = 0;

            virtual void Disconnect() = 0;

            virtual void ClientDataReady(bool isNotification) = 0;

        protected:


    };
}