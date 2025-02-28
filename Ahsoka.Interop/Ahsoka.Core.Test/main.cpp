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
 
int responseCount = 0;
int requestCount = 10;
ReadyWaiter waiter;

void NotificationRecieved(TestMessageTypes transportId, AhsokaServiceFramework::AhsokaMessageType_t& message)
{
    if (transportId == EchoTestNotification)
    {
        auto callbackMessage = reinterpret_cast<AhsokaCore::ServiceConfiguration&>(message);
        std::cout << callbackMessage.service_name() << " Notification Received" << std::endl;
    
        responseCount++;
        if (responseCount == requestCount)
            waiter.SetReady();
    }
}

void RunTest()
{
    AhsokaCore::ServiceConfiguration localConfig; 
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
    for (size_t i = 0; i < requestCount; i++)
    {    
        auto newConfig = client->EchoConfig(localConfig, 5000);
        std::cout << newConfig->service_name() << " Message Received" << std::endl;
    }
    
    // Block until message recieved.
    waiter.WaitForReady();

    // Stop Client
    client->Stop();

    // Start Service
    service->Stop();

    delete client;
    delete service;
}

int main()
{
    RunTest();
    RunTest();
    RunTest();
   
    return 0;
}
