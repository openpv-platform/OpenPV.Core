#include <string>
#include <iostream>
#include "AhsokaServices.h"
#include "IotTests.hpp"
#include "SystemTests.hpp"
#include "SparkTests.hpp"
#include <filesystem>

using namespace std;

const std::string pathToExecutable = "./CommandLine/Ahsoka.CommandLine";
 
int main(int argc, char *argv[])
{
    while (true)
    {
        cout << endl << "Ahsoka Service Tests" << endl << endl;
        cout << "Select a Test to Run" << std::endl;
        cout << "   1: Service Tests" << std::endl;
        cout << "   2: Iot Tests" << std::endl;
        cout << "   3: Spark Test" << std::endl;
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


