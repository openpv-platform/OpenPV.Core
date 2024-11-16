#pragma once
#include <vector>
#include <string>
#include <queue>
#include <mutex>
#include <chrono>
#include <thread>
#include <random>
#include <climits>
#include <iostream>
#include <condition_variable>
#include "ReadyWaiter.h"
#include <zmq.hpp>
#include <zmq_addon.hpp>
#include "IAhsokaEndPoint.h"
#include "IAhsokaSocket.h"
#include "BlockingQueue.h"

namespace AhsokaServiceFramework
{
    class ClientRegistration;

    class AhsokaServiceSocket : public IAhsokaSocket
    {
        public:

            bool GetIsConnected();
            void SetEndPoint(IAhsokaEndPoint* endpoint);       
            void Connect();
            void Disconnect();
            void SendMessage(std::string header, std::string data,  std::string clientId, bool expectResponse);
            bool ReceiveMessage(std::string& header, std::string& data,  std::string& clientId, bool isOutOfBand, int timeoutInMs = INT_MAX);
     
            void RegisterClient(ClientRegistration* registration);
            void DeregisterClient(ClientRegistration* registration);

        private:

            bool IsConnected;
            bool IsListening;
            bool IsNotifying;

            // Socket Thread Items            
            ReadyWaiter socketThreadReady;
            std::thread* socketThread;
            std::vector<std::string> clientList;
            void ManageSocket();
            
            // Notification Reciever Items
            ReadyWaiter notificationThreadReady;
            std::thread* notificationThread;
            void ManageNotifications();
            AhsokaServiceFramework::BlockingQueue<SocketMessage> notificationMessages;

            zmq::socket_t* socket;
            zmq::context_t *context;
            zmq::context_t& GetContext();

            std::string GetPairAddress();
    };

    
}