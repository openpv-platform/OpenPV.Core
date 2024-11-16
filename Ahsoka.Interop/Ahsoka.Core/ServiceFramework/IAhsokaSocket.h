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

#include "IAhsokaEndPoint.h"

namespace AhsokaServiceFramework
{

    class SocketMessage
	{
		public:

			std::string message;
			std::string header;
            bool isNotification;
	};


    class IAhsokaSocket
    {
        public:

            virtual bool GetIsConnected() = 0;
            virtual void Connect() = 0;
            virtual void Disconnect() = 0; 
            virtual void SendMessage(std::string header, std::string data,  std::string clientId, bool sendOutOfBand) = 0;
            virtual bool ReceiveMessage(std::string& header, std::string& data,  std::string& clientId, bool isNotification, int timeoutInMs = INT_MAX) = 0;
         
        protected:

            std::string GetDataChannel();
            std::string GetDataListenChannel();
            std::string CreateGUID();
            IAhsokaEndPoint* endPoint;

    };



}