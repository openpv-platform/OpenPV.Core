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

            virtual ~IAhsokaEndPoint() {}

            virtual AhsokaCore::ServiceConfiguration& GetRuntimeConfiguration() = 0;

            virtual void SetRuntime(AhsokaRuntime*) = 0;
           
            virtual void Start() = 0;

            virtual void Stop() = 0;

            virtual void RecieveMessage(bool isNotification) = 0;

        protected:


    };
}