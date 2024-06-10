#include <iostream>
#include <string>
#include "AhsokaServices.h"
#include "IotTests.hpp"
#include "SystemTests.hpp"
#include "SparkTests.hpp"
#include "NetworkTests.hpp"
#include "CanTests.hpp"
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

    while (true)
    {
        cout << endl << "Ahsoka Service Tests" << endl << endl;
        cout << "Select a Test to Run" << std::endl;
        cout << "   1: Service Tests" << std::endl;
        cout << "   2: Iot Tests" << std::endl;
        cout << "   3: Spark Test" << std::endl;
        cout << "   4: Network Test" << std::endl;
        cout << "   5: Can Test" << std::endl;
        cout << "   9: Exit Test" << std::endl << std::endl;

        int test;
        cin >> test;
        
        try
        {
            switch (test)
            {
                case 1:
                {
                    SystemTests system;
                    system.RunTests(pathToExecutable);
                    break;
                }
                case 2:
                {
                    IotTests iot;
                    iot.RunTests(pathToExecutable);
                    break;
                }
                case 3:
                {
                    SparkTests spark;
                    spark.RunTests(pathToExecutable);
                    break;
                }
                case 4:
                {
                    NetworkTests network;
                    network.RunTests(pathToExecutable);
                    break;
                }
                case 5:
                {
                    CanTests canTest;
                    canTest.RunTests(pathToExecutable);
                    break;
                }
                case 9:
                    
                    return 0;
            }                
        }
        catch (std::exception& e)
        {
            cout << "ERROR: " << e.what() << endl;
        }
    }

    return 0;
}
