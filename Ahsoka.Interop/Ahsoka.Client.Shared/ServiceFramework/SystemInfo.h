#pragma once

#include <iostream>
#include <string>
#include <unistd.h>
#include <fstream>
#include "TargetPathInfo.h"

using namespace AhsokaSystem;

namespace AhsokaSystem
{
    class HardwareInfo
    {
        public:
        
            std::string GetRootPath();
            std::string GetPlatformFamily();
            std::string GetPlatformQualifier();
            AhsokaSystem::TargetPathInfo& GetTargetPathInfo();

            HardwareInfo();

        private: 

            std::string rootPath;
            std::string family;
            std::string qualifier;
            AhsokaSystem::TargetPathInfo *info;
    };
   
    class SystemInfo
    {
        public:
        
            static HardwareInfo GetHardwareInfo()
            {
                static AhsokaSystem::HardwareInfo info;
                return info;
            };
            

        private: 


    };


    



}