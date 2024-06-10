#include <fstream>
#include <iostream>
#include <string>
#include <mutex>
#include <condition_variable>

#include "AhsokaServices.h"

using namespace google::protobuf::util;

class IotTests
{
    public:


        void RunTests(std::string pathToServiceExe)
        {
            SystemServiceClient client;
            IotServiceClient iotClient;

            auto runtime = AhsokaRuntime::CreateBuilder()
                .AddClient(&client)
                .AddClient(&iotClient)
                .SetServiceExecutable(pathToServiceExe)
                .StartWithExternalServices(); // Connect to Already Running Runtime

            std::cout << client.Echo("Server Ready") << ::endl;
          
            std::cout << endl << "Running IOT Tests"  << ::endl;
          
            auto clientInfo = TestConnection(iotClient);
            if (clientInfo.get() != 0)
            {
                std::cout << "  Connected to " << url << std::endl;

                TestSubscription(iotClient, *clientInfo.get());

                TestDisconnect(iotClient, *clientInfo.get());
            }
    
            std::cout << "IOT Test Complete" << std::endl;

            // Stop Server
            client.RequestServiceShutdown();

            // Stop Local Services
            runtime.RequestShutdown();
            runtime.WaitForShutdown();

            std::cout << "Shutdown Completed" << std::endl;

        }

    private:

        std::mutex waitMutex;
        std::condition_variable condVar;

        TopicMessage callbackMessage;
        bool messageReceived = false;

        const string topic = "AhsokaTestTopic";
        const string url = "18144f318ca0488dacc077721d7778ee.s2.eu.hivemq.cloud";
        const int port = 8883;
        const string deviceName = "AhsokaDevice";
        const string devicePassword = "bedzyq-cimwez-Nobxe0";

        void MessageReceived();
        
        unique_ptr<ClientConnectionInfo> TestConnection(IotServiceClient& client)
        {
            std::cout << "  Connecting to " << url << std::endl;

            // Create Client
            ClientInfo connectInfo;
            connectInfo.set_broker_address(url);
            connectInfo.set_broker_port(port);
            connectInfo.set_device_user_name(deviceName);
            connectInfo.set_device_password(devicePassword);

            // Fix Password and ReConnect...wait for ConnectedState
            return client.CreateClient(connectInfo);
        }

        void TestSubscription(IotServiceClient& client, ClientConnectionInfo& clientState)
        {
            // Create a Callback Lambda to pass to the notification handler...we will remove at the end.
            auto callback = [this](AhsokaIot::IotMessageTypes::Ids transportId, AhsokaServiceFramework::AhsokaMessageType_t& message)
            {
                if (transportId == IotMessageTypes_Ids_TOPIC_RECEIVED_NOTIFICATION)
                {
                    // Wake up and notify the main thread when thte message arrives
                    std::lock_guard<std::mutex> lock(waitMutex);
                   
                    // Copy for Later
                    callbackMessage = reinterpret_cast<TopicMessage&>(message);
                    messageReceived = true;

                    condVar.notify_one();
                }
            };

            // Listen to Reply            
            client.RegisterListener(callback);
            
            std::cout << "  Subscribing Client to " << topic << std::endl;
           
            TopicSubscription topicSub;
            topicSub.set_client_id(clientState.client_id());
            topicSub.set_topic_name(topic);
            topicSub.set_quality_of_service(EXACTLY_ONCE);
            client.SubscribeTopic(topicSub);

            std::cout << "  Sending Message to " << topic << std::endl;
           
            TopicMessage message;
            message.set_topic_name(topic);
            message.set_client_id(clientState.client_id());
            message.set_payload("Test Message from MQTT");
            client.PublishTopic(message);

            {
                std::cout << "  Waiting for Reply" << std::endl;
                std::unique_lock<std::mutex> lock(waitMutex);
                condVar.wait(lock, [this] {return messageReceived;});
                std::cout << "  Message Received: " << callbackMessage.payload() << std::endl;
            }
        
            std::cout << "  Unsubscribing Client to " << topic << std::endl;
            client.UnsubscribeTopic(topicSub);
    
            client.RegisterListener(0);
        }
                
                  
        void TestDisconnect(IotServiceClient& client, ClientConnectionInfo& clientState)
        {
            // Disconnect
            ClientMessage clientMessage;
            clientMessage.set_client_id(clientState.client_id());
            auto disconnect = client.DestroyClient(clientMessage);
  
            std::cout << "  Disconnecting Client from " << url << std::endl << std::endl;
        }
};
