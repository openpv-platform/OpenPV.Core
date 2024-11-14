#pragma once

#include <iostream>
#include <string>
#include <unistd.h>
#include <cstdio>
#include "TargetPathEnums.h"

using namespace std;
using namespace AhsokaSystem;

namespace AhsokaSystem
{
    class TargetPathInfo
    {
        public:

            TargetPathInfo(string platformRootPath);
            string GetRootPaths(RootPaths path);;
            string GetInstallerPath(InstallerPaths path, bool factory = true);
            string GetFactoryPath(FactoryPaths path);
            string GetServiceDataPath(ServiceDataPaths path);
            string GetServiceDataPath(string serviceName);
            string GetAhsokaPath(string path);

        private: 

            string rootPath;

    };

}