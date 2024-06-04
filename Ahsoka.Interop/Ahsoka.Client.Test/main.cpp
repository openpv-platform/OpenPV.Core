#include <iostream>
#include <string>
#include "TestServiceClient.h"
#include <filesystem>


using namespace std;

#ifdef __arm__
    const std::string pathToExecutable = "/usr/local/Ahsoka/factory/commandline/Ahsoka.CommandLine";
#elif defined(_WIN32)
    const std::string pathToExecutable = "../../../../../../Ahsoka.Platform/PlatformSupport/Windows64_TargetSupport/CommandLine/Ahsoka.CommandLine.exe";
#else
    const std::string pathToExecutable = "/workspaces/Ahsoka.Platform/Ahsoka.Platform/PlatformSupport/Ubuntu64_TargetSupport/CommandLine/Ahsoka.CommandLine";              
#endif
 
int main()
{
    TestServiceClient();

    return 0;
}
