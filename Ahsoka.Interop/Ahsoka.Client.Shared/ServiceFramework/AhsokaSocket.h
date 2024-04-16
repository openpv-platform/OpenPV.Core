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

#include <zmq.hpp>
#include <zmq_addon.hpp>
#include "IAhsokaEndPoint.h"

namespace AhsokaServiceFramework
{
    
    class AhsokaSocket
    {
        private:
            std::string dataChannel();
            std::string dataListenChannel();
            std::string notificationChannel();
            std::string notificationListenChannel();

        public:

            bool GetIsConnected();
            void SetEndPoint(IAhsokaEndPoint* endpoint);       
            void Connect(zmq::context_t& context);
            void Disconnect();
            void SendMessage(const std::string& header, const std::string& data, bool isOutOfBand);
            bool ReceiveMessage(std::string& header, std::string& data, bool isOutOfBand, int timeoutInMs = INT_MAX);
            std::string GetClientId();

        private:

            std::string clientId;
            zmq::context_t *context;
            
            bool IsConnected;
            bool IsListening;

            zmq::socket_t* socket;
            zmq::socket_t* notificationSocket;
            IAhsokaEndPoint* endPoint;
            std::thread* listenerThread;

            zmq::context_t& GetContext();
            void ManageSocket();           
 			std::string CreateGUID();
    };
}