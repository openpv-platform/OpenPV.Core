#pragma once

#include <memory>
#include "Services.pb.h"
#include "Configuration.pb.h"
#include "AhsokaRuntime.h"
#include "AhsokaMessagesBase.h"
#include "IAhsokaSocket.h"

namespace AhsokaServiceFramework
{
	template <typename TTransportID>
	class AhsokaBaseEndPoint : public IAhsokaEndPoint
	{
		public:

			AhsokaBaseEndPoint() {}
            
            virtual ~AhsokaBaseEndPoint() {}

			bool GetIsConnected()
			{
				if (currentRuntime)
					return socket->GetIsConnected();
				return false;		
			}

			void Connect()
			{
				if (currentRuntime)
				{
					socket->Connect();
					OnEndPointConnected();
				}					
			}

			void Disconnect()
			{
				OnEndPointDisconnected();
				socket->Disconnect();
			}

		    AhsokaCore::ServiceConfiguration& GetRuntimeConfiguration() 
			{
				return serviceConfig;
			}


 			template <typename TEndPoint>
            [[deprecated("Use 'this' instead.")]]
			TEndPoint* GetEndPoint() { return static_cast<TEndPoint*>(this); }

			void Start()
			{
				AhsokaRuntime::CreateBuilder()
						.AddEndPoint(this)
						.StartLocalRuntime();
			}

			void Stop()
			{
				try
				{
					this->currentRuntime->RequestShutdown();
				}
				catch(const std::exception& e) {}
				
				this->currentRuntime->WaitForShutdown();
			}

		protected:

		 	IAhsokaSocket * socket;
			AhsokaMessagesBase messageFactory;
			bool isServiceEndPoint;

			virtual IAhsokaSocket* CreateSocket(AhsokaCore::ServiceConfiguration& config) = 0;

			void SetRuntime(AhsokaRuntime* runtime) {currentRuntime = runtime;}

			AhsokaBaseEndPoint(AhsokaCore::ServiceConfiguration& config, AhsokaMessagesBase& messages)
				:serviceConfig(config),
				 messageFactory(messages)
			{
				// Validate that we are using an Enumeration TYPE
				static constexpr bool is_enum = std::is_enum<TTransportID>::value;
				currentRuntime = 0;
			}

			void SendMessageInternal(AhsokaMessageHeader& messageHeader, AhsokaMessage_t messageObject = 0, bool isNotifcation = false)
			{
				if (socket->GetIsConnected())
				{
					if (messageObject == 0)
						messageObject = new EmptyNotification();

					std::string clientId = messageHeader.client_id();
					socket->SendMessage(messageHeader.SerializeAsString(), messageObject->SerializeAsString(), clientId , isNotifcation);

					messageSendCount++;
				}
				else { }
			}

		    bool ReceiveMessage(AhsokaMessageHeader *& messageHeader, AhsokaMessageType_t *& messageObject, 
					bool isOutOfBand, int timeoutInMs = INT_MAX)
		    {
				messageReceivedCount++;

				std::string headerData;
				std::string messageData;
				std::string clientId;

				// Try to Recieve a Message
				if (!socket->ReceiveMessage(headerData, messageData, clientId, isOutOfBand, timeoutInMs))
					return false;

				// Decode Request Header
				messageHeader = new AhsokaMessageHeader();
				messageHeader->ParseFromString(headerData);
				messageHeader->set_client_id(clientId);
				
				// Lookup and Decode Message
				messageObject = messageFactory.CreateMessage<AhsokaMessageType_t>(messageHeader->transport_id(), messageData, isServiceEndPoint);
		
				return (messageObject != 0);
			}
			
			virtual void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message) = 0;

			virtual void OnEndPointConnected() {}

			virtual void OnEndPointDisconnected() {}

			void UpdateConfiguration(AhsokaCore::ServiceConfiguration config) {serviceConfig = config;}

	private:
			
			int messageSendCount = 0;
			int messageReceivedCount = 0;
			
			AhsokaRuntime* currentRuntime;
			AhsokaCore::ServiceConfiguration serviceConfig;
			
	};
}
