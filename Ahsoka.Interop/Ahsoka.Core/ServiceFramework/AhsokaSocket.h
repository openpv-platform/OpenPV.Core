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
#include "BlockingQueue.h"

namespace AhsokaServiceFramework
{
    class SocketMessage
	{
		public:

			std::string message;
			std::string header;
            bool expectResponse;
	};

    class AhsokaSocket
    {
        private:
            std::string dataChannel();
            std::string dataListenChannel();

        public:

            bool GetIsConnected();
            void SetEndPoint(IAhsokaEndPoint* endpoint);       
            void Connect(zmq::context_t& context);
            void Disconnect();
            void SendMessage(std::string header, std::string data, bool expectResponse);
            bool ReceiveMessage(std::string& header, std::string& data, bool isOutOfBand, int timeoutInMs = INT_MAX);
            std::string GetClientId();
            std::string GetNotificationClientId();

        private:

            std::string clientId;
            std::string notificationClientId;
            zmq::context_t *context;
            
            bool IsConnected;
            bool IsListening;

            zmq::socket_t* socket;
            zmq::socket_t* notificationSocket;
            
            IAhsokaEndPoint* endPoint;
            std::thread* listenerThread;
            std::thread* senderThread;

            AhsokaServiceFramework::BlockingQueue<SocketMessage> outgoingMessages;

            zmq::context_t& GetContext();
            void ManageListener();  
            void ManageSender();       
            void SendMessage(const std::string& header, const std::string& data);  
 			std::string CreateGUID();
    };
}