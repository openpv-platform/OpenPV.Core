#pragma once

#include <iostream>
#include <string>
#include "AhsokaServices.h"

class SystemTests
{
    public:
        void RunTests(std::string pathToServiceExe)
        {
            SystemServiceClient client;

            auto runtime = AhsokaRuntime::CreateBuilder()
                .AddEndPoint(&client)
                .SetServiceExecutable(pathToServiceExe)
                .StartWithExternalServices();

            std::cout << client.Echo("Echo Test ") << std::endl;

            auto hardwareInfo = client.GetHardwareInformation();
            
            std::cout << std::endl << "Hardware Connected to (" << hardwareInfo.get()->application_name() << ") "
                << std::endl << "Device - " << hardwareInfo.get()->serial_number() << " - " << hardwareInfo.get()->part_number()
                << std::endl;

            
            std::cout << std::endl << "Setting Brightness value 50%: ";
            AhsokaSystem::BrightnessInfo brightness;   
            brightness.set_percentage(50);
            std::cout << client.SetBrightness(brightness)->percentage() << "%" << std::endl;

            std::cout << std::endl << "Setting Brightness value 75%: ";
            brightness.set_percentage(75);
            std::cout << client.SetBrightness(brightness)->percentage() << "%" << std::endl;

            std::cout << std::endl << "Getting Brightness value: " 
                << client.GetBrightness().get()->percentage()
                << std::endl;

            auto usbInfo = client.GetUsbInfo();
            std::cout << std::endl << "USB Info: " 
                << usbInfo.get()->mode() << " " << usbInfo.get()->mount_point()
                << std::endl;   


            client.RequestServiceShutdown();
            runtime.RequestShutdown();
            runtime.WaitForShutdown();

            std::cout << "Shutdown Completed" << std::endl;

        }
};
