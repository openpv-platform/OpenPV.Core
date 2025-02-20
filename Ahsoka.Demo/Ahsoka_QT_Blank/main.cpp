#include "AhsokaServices.h"
#include <iostream>
#include <string>

#include <QApplication>

int main(int argc, char *argv[])
{

    AhsokaSystem::SystemServiceClient client;

    auto runtime = AhsokaRuntime::CreateBuilder()
        .AddEndPoint(&client)
        .SetServiceExecutable("./CommandLine/Ahsoka.CommandLine")
        .StartWithExternalServices();

    AhsokaRuntime::ReleaseStartup();
    std::cout << client.Echo("Echo Test ") << std::endl;

    auto hardwareInfo = client.GetHardwareInformation();

    std::cout << std::endl << "Hardware Connected to (" << hardwareInfo.get()->application_name() << ") "
        << std::endl << "Device - " << hardwareInfo.get()->serial_number() << " - " << hardwareInfo.get()->part_number()
        << std::endl;


    AhsokaSystem::BrightnessInfo brightness;
    brightness.set_percentage(75);
    client.SetBrightness(brightness);

    std::cout << std::endl << "Setting Brightness value: "
        << client.GetBrightness().get()->percentage()
        << std::endl;

    runtime.RequestShutdown();
    runtime.WaitForShutdown();

    std::cout << "Shutdown Completed" << std::endl;

    return 0;

}
