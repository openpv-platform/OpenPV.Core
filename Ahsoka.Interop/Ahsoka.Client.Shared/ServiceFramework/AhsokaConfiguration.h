#pragma once
#include <memory>
#include "Configuration.pb.h"

using namespace std;
using namespace AhsokaSystem;
    
namespace AhsokaServiceFramework
{
    class AhsokaConfiguration
    {
        public:

            static void LoadConfiguration(const string& configurationFile);
            static ServiceConfiguration GetServiceConfiguration(const string& serviceName);

        private:

            static RuntimeConfiguration runtimeConfig;
    };
}