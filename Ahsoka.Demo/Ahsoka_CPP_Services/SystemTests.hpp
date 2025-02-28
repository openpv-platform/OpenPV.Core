#pragma once

#include <iostream>
#include <string>
#include "AhsokaServices.h"
#include "AhsokaRuntimeProcess.h"

class SystemTests
{
    public:
        void RunTests(std::string pathToServiceExe)
        {
            auto serviceProcess = AhsokaRuntimeProcess::StartServiceExecutable(pathToServiceExe);

            SystemServiceClient client;
            client.Start();    

            std::cout << client.Echo("Echo Test ") << std::endl;

            auto hardwareInfo =  client.GetHardwareInformation();
            std::cout << std::endl << "Hardware Connected to (" << hardwareInfo->application_name() << ") "
            << std::endl << "Device - " << hardwareInfo->serial_number() << " - " << hardwareInfo->part_number()
            << std::endl;
            
            std::cout << std::endl << "Setting Brightness value 50%: ";
            AhsokaSystem::BrightnessInfo brightness;   
            brightness.set_percentage(50);
            std::cout <<  client.SetBrightness(brightness)->percentage() << "%" << std::endl;

            std::cout << std::endl << "Setting Brightness value 75%: ";
            brightness.set_percentage(75);
            std::cout <<  client.SetBrightness(brightness)->percentage() << "%" << std::endl;

            std::cout << std::endl << "Getting Brightness value: " 
                <<  client.GetBrightness().get()->percentage()
                << std::endl;

            // Request Service Stop 
            client.RequestServiceShutdown();

            // Stop Client
            client.Stop();

            serviceProcess->EndProcess();

            std::cout << "Shutdown Completed" << std::endl;
        }
};
