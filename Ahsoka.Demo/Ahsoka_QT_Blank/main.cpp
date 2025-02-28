#include "AhsokaServices.h"
#include "AhsokaRuntimeProcess.h"
#include <iostream>
#include <string>

#include <QApplication>

int main(int argc, char *argv[])
{

    auto serviceProcess = AhsokaRuntimeProcess::StartServiceExecutable("./CommandLine/Ahsoka.CommandLine");

    AhsokaSystem::SystemServiceClient client;
    client.Start();

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

    client.RequestServiceShutdown();
    client.Stop();

    serviceProcess->EndProcess();

    std::cout << "Shutdown Completed" << std::endl;

    return 0;

}
