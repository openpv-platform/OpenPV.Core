#pragma once

#include <iostream>
#include <string>
#include "AhsokaServices.h"
#include "CanViewModelBase.h"
#include <unistd.h>
#include "CanModels.h"

class CanTests
{
    public:
       
        void RunTests(std::string pathToServiceExe)
        {
            // Create a Temp Config 
            std::string tempConfig = CreateCanAppConfiguration();

            CanServiceClient client;

            auto runtime = AhsokaRuntime::CreateBuilder()
                .AddClient(&client)
                .SetServiceExecutable(pathToServiceExe)
                .StartWithExternalServices();

            TestCanViewModel();

            TestCanService(client);
           
            runtime.RequestShutdown();
            runtime.WaitForShutdown();

            // Delete the Configuration
            std::remove(tempConfig.c_str());

            std::cout << "Shutdown Completed" << std::endl;
        }

        void TestCanService(CanServiceClient& client)
        {
            // Create Callbacks Lambda to pass to listen for Messages
            auto stateCallBack = [this](CanState& message)
            {
                // Wake up and notify the main thread when thte message arrives
                std::lock_guard<std::mutex> lock(waitMutex);
                state = message;
                stateReceived = true;
                condVar.notify_one();
            };

            // LSetup State Listener         
            client.RegisterStateListener(stateCallBack);

            // Create Callbacks Lambda to pass to listen for Messages
            auto messageCallBack = [this](CanMessageDataCollection& message)
            {
                // Wake up and notify the main thread when thte message arrives
                std::lock_guard<std::mutex> lock(waitMutex);
                for (auto it : message.messages())
                    messages.push_back(it);
                condVar.notify_one();
            };

            // Listen to Reply            
            client.RegisterCanListener(messageCallBack);

            // Open and Verify we recieved the Port Configuration
            client.OpenCommunicationChannel();
            auto portConfig = client.GetPortConfiguration();
            uint id = portConfig.messageconfiguration().messages(0).id();
            
            // Other ID for Testing.
            uint otherId = id + 1;

            // Verify we recieved the State Setup (Address Claim etc.)
            {
                std::cout << "  Waiting for State Reply" << std::endl;
                std::unique_lock<std::mutex> lock(waitMutex);
                condVar.wait(lock, [this] {return stateReceived;});
            }

            // Test Send Message Calls
            auto canMessage = CreateTestViewModel();
            canMessage.SetTestSigned(1);
            client.SendCanMessages(1, canMessage);

            canMessage = CreateTestViewModel();
            canMessage.SetTestSigned(2);
            client.SendCanMessages(1, canMessage);
          
            // Build Message from Model for Recurring Message
            canMessage = CreateTestViewModel();
            canMessage.SetTestSigned(3);
            auto canData = canMessage.CreateCanMessageData();

            // Build Recurring Message - Note we Swap the Can Data 
            RecurringCanMessage recurringMessage;
            recurringMessage.set_can_port(1);
            recurringMessage.set_timeout_before_update_in_ms(500);     
            recurringMessage.set_transmit_interval_in_ms(100);
            recurringMessage.mutable_message()->Swap(&canData);
            client.SendRecurringCanMessage(recurringMessage);
          
            // Wait for X Messages to Return
            {
                std::cout << "  Waiting for Message Reply" << std::endl;
                std::unique_lock<std::mutex> lock(waitMutex);
                condVar.wait(lock, [this] {return messages.size() >= 3;});
            }

            // First Message
            auto returnMessage = new TestCanModel(messages[0]);
            if (returnMessage->GetTestSigned() != 1)
                std::cout << "Round Trip Message 1 Did Not Match" << std::endl;
            
            returnMessage = new TestCanModel(messages[1]);
            if (returnMessage->GetTestSigned() != 2)
                std::cout << "Round Trip Message 2 Did Not Match" << std::endl;
            
            returnMessage = new TestCanModel(messages[2]);
            if (returnMessage->GetTestSigned() != 3)
                std::cout << "Round Trip Message 3 Did Not Match" << std::endl;

            std::cout << "Closing Communications" << std::endl;
            client.CloseCommunicationChannel();
        }

        TestCanModel CreateTestViewModel()
        {
            TestCanModel model;
            model.SetTestSigned(64);
            model.SetTestUnsigned(256);
            model.SetTestEnum(TestEnumValues::Test2EnumTwo);
            model.SetTestFloat(12345.0f);
            model.SetTestDouble(54321.0f);
            return model;
        }

        void TestCanViewModel()
        {
            TestCanModel model = CreateTestViewModel();

            if (model.GetTestSigned() != 64)
                std::cout << "Test Signed Did Not Match" << std::endl;
          
            if (model.GetTestUnsigned() != 256)
                std::cout << "Test Unsigned Did Not Match" << std::endl;
          
            if (model.GetTestEnum() != TestEnumValues::Test2EnumTwo)
                std::cout << "Test Enum Did Not Match" << std::endl;
                
             if (model.GetTestFloat() != 12345.0f)
                std::cout << "Test Float Did Not Match" << std::endl;

            if (model.GetTestDouble() != 54321.0f)
                std::cout << "Test Double Did Not Match" << std::endl;
               
            // Get UnScaled Value..should be half of Normal Value
            uint32_t value = model.GetRawValue<uint32_t>(TestCanModel::Properties::TestUnsigned);
            if (value!= 128)
                std::cout << "Test UnScaled Failed" << std::endl;

            // Test Values Get Unset.
            model.SetTestUnsigned(0);
            if (model.GetTestUnsigned() != 0)
               std::cout << "Reset Unsigned Failed" << std::endl;
          
            auto data = model.CreateCanMessageData();
            if (data.id() != 500)
               std::cout << "ID Did Not Match" << std::endl;
            if (data.dlc() != 16)
               std::cout << "Dlc did not  Match" << std::endl;

            // Fetch the Property Object from the Metadata Factory usind the ID and Property Enum
            auto values = CanModelMetadata::GetMetadata(data.id());
            auto prop = values[TestCanModel::Properties::TestSigned];
            
            // Set the Property and Verify it Returns.
            prop.SetValue<int>(data.mutable_data(), 3);
            int resultValue = prop.GetValue<int>(data.mutable_data());

            if (resultValue != 3)
               std::cout << "Metadata Generated Value did not match" << std::endl;

            resultValue = prop.GetSignalId();
            if (resultValue != 12)
               std::cout << "Metadata Signal is Incorrect" << std::endl;


            // Fetch the Property Object from the Metadata Factory usind the ID and Property Enum
            auto propInfo = CanModelMetadata::GetPropertyInfo(data.id(),TestCanModel::Properties::TestSigned, true);
        }


        std::string CreateCanAppConfiguration()
        {
             // Write a Configuration File
            std::string pathToAppConfig = "/home/%USER%/Ahsoka/current/.coprocessor/applicationConfiguration.pbuff";
            system("mkdir -p /home/%USER%/Ahsoka/current/.coprocessor/");
            
            // Build Config
            CanApplicationCalibration canConfig;
            CanPortConfiguration* portConfig = canConfig.mutable_canportconfiguration();
          
            // Create Comm Config
            CommunicationConfiguration* commConfig = portConfig->mutable_communicationconfiguration();          
            MessageConfiguration* messageConfig = portConfig->mutable_messageconfiguration();

            PortDefinition* portDef = messageConfig->add_ports();
            portDef->set_port(1);
            portDef->set_can_interface(CanInterface::Coprocessor);

            // Create Comm Config
            NodeDefinition* node = messageConfig->add_nodes();
            node->set_node_type(Self);
            node->add_ports(1);

            MessageDefinition* messageDef = messageConfig->add_messages();
            messageDef->set_id(500);
            messageDef->set_filter_receipts(true);
            messageDef->set_timeout_ms(1000);
            messageDef->set_dlc(64);
            messageDef->add_transmit_nodes(-1);
            messageDef->add_transmit_nodes(1);

            fstream output(pathToAppConfig, ios::out | ios::trunc | ios::binary);
            canConfig.SerializeToOstream(&output);
            output.flush();
            output.close();

            return pathToAppConfig;
        }

    private: 

        std::mutex waitMutex;
        std::condition_variable condVar;
        std::vector<CanMessageData> messages;
        CanState state;
        bool stateReceived = false;
};