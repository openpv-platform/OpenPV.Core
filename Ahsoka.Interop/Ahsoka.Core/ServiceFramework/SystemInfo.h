#pragma once

#include <iostream>
#include <string>
#include <unistd.h>
#include <fstream>
#include "TargetPathInfo.h"

using namespace AhsokaCore;

namespace AhsokaCore
{
    class HardwareInfo
    {
        public:
        
            std::string GetRootPath();
            std::string GetPlatformFamily();
            std::string GetPlatformQualifier();
            AhsokaCore::TargetPathInfo& GetTargetPathInfo();

            HardwareInfo();

        private: 

            std::string rootPath;
            std::string family;
            std::string qualifier;
            AhsokaCore::TargetPathInfo *info;
    };
   
    class SystemInfo
    {
        public:
        
            static HardwareInfo GetHardwareInfo()
            {
                static AhsokaCore::HardwareInfo info;
                return info;
            };
            

        private: 


    };


    



}