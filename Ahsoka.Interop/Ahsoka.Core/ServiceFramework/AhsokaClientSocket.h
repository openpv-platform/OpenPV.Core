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
    class AhsokaClientSocket : public IAhsokaSocket
    {
        public:

            bool GetIsConnected();
            void SetEndPoint(IAhsokaEndPoint* endpoint);       
            void Connect();
            void Disconnect();
            void SendMessage(std::string header, std::string data, std::string clientId, bool isNotifcation);
            bool ReceiveMessage(std::string& header, std::string& data, std::string& clientId, bool isNotifcation, int timeoutInMs = INT_MAX);
            std::string GetClientId();
            std::string GetNotificationClientId();

        private:

            std::string clientId;
            std::string notificationClientId;
            zmq::context_t *context;
            
            bool IsConnected;
            bool IsListening;
            bool IsSending;

            // Socket Thread Items
            ReadyWaiter socketThreadReady;
            std::thread* socketThread;
            zmq::socket_t* socket;
            void ManageSocket();      

            ReadyWaiter notificationThreadReady;
            std::thread* notificationThread;
            zmq::socket_t* notificationSocket;
            void ManageNotifications();       
           
            AhsokaServiceFramework::BlockingQueue<SocketMessage> outgoingMessages;

            zmq::context_t& GetContext();
            
            void SendMessage(const std::string& header, const std::string& data);  

    };

    
}