#include "json.cpp"
#include "SystemInfo.h"
#include "TargetPathInfo.h"


using json = nlohmann::json;

namespace AhsokaSystem
{
    std::string HardwareInfo::GetRootPath()
    {
        return rootPath;
    }
        
    std::string HardwareInfo::GetPlatformFamily()
    {
        return family;
    }

    std::string HardwareInfo::GetPlatformQualifier()
    {
        return qualifier;
    }

    AhsokaSystem::TargetPathInfo& HardwareInfo::GetTargetPathInfo()
    {
        return *info;
    };

    HardwareInfo::HardwareInfo()
    {
        
        // Currently support Unix / Linux Only 
        std::ifstream hardwareInfoFile("/usr/local/Ahsoka/factory/hardware/HardwareConfiguration.json");
        json data = json::parse(hardwareInfoFile);
        json factoryInfo = data["factoryInfo"];
        rootPath = factoryInfo["rootPath"];
        family = factoryInfo["platformFamily"];
        qualifier = factoryInfo["platformQualifier"];

        info = new AhsokaSystem::TargetPathInfo(rootPath);
    };


}