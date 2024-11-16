#include <iostream>
#include <string>
#include <mutex>
#include <condition_variable>
#include "TestServiceClient.h"
#include "TestService.h"
#include <filesystem>
#include "Configuration.pb.h"
#include <unistd.h>

using namespace std;
 
ReadyWaiter waiter;

void NotificationRecieved(TestMessageTypes transportId, AhsokaServiceFramework::AhsokaMessageType_t& message)
{
     if (transportId == EchoTestNotification)
    {
        std::cout << "Notification Received" << std::endl;
        waiter.SetReady();
    }
}

int main()
{
    AhsokaSystem::ServiceConfiguration localConfig; 
    localConfig.set_service_name(TestServiceName);
    localConfig.set_tcp_listen_address("*");
    localConfig.set_tcp_connection_address("localhost");
    localConfig.set_data_channel(5000);
    
    // Create Service
    TestService * service = new TestService(localConfig);
    service->Start();

    // Start Client 
    TestServiceClient * client = new TestServiceClient(localConfig);
    client->Start();
    client->NotifyMessageReceived(NotificationRecieved);

    // Call Example Function...this will also send a notification
    auto newConfig = client->EchoConfig(localConfig, 5000);
    
    // Block until message recieved.
    waiter.WaitForReady();

    // Stop Client
    client->Stop();

    // Start Service
    service->Stop();
   
    return 0;
}
