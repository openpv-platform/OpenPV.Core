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

            // Socket Thread / Socket.
            // This thread manages the recieve and send of message on the main 
            // router socket            
            zmq::socket_t* socket;
            ReadyWaiter socketThreadReady;
            std::thread* socketThread;
            std::vector<std::string> clientList;
            void ManageSocket();
            
            int socketTID = -1;

            // Notification Reciever Items.   
            // This Thread manages any out of band notifications that we recieve
            // and uses a pair socket to route them to the socket thread to be send when available.
            ReadyWaiter notificationThreadReady;
            std::thread* notificationThread;
            void ManageNotifications();
            
            AhsokaServiceFramework::BlockingQueue<SocketMessage> notificationMessages;

            std::string GetPairAddress();
            void SendMessageToTransport(std::string header, std::string data, std::string clientId);
   
    };

    
}