#pragma once

#include <memory>
#include "Services.pb.h"
#include "Configuration.pb.h"
#include "AhsokaRuntime.h"
#include "AhsokaMessagesBase.h"
#include "AhsokaSocket.h"

namespace AhsokaServiceFramework
{	
	template <typename TTransportID>
	class AhsokaBaseEndPoint : public IAhsokaEndPoint
	{
		public:

			void Connect()
			{
				if (currentRuntime)
				{
					socket.Connect(currentRuntime->GetZMQContext());
					OnEndPointConnected();
				}					
			}

			void Disconnect()
			{
				socket.Disconnect();
				OnEndPointDisconnected();
			}

		    AhsokaSystem::ServiceConfiguration& GetRuntimeConfiguration() 
			{
				return serviceConfig;
			}

		protected:
		 	AhsokaSocket socket;
			AhsokaMessagesBase messageFactory;

			void SetRuntime(AhsokaRuntime* runtime) {currentRuntime = runtime;}

			AhsokaBaseEndPoint(AhsokaSystem::ServiceConfiguration& config, AhsokaMessagesBase& messages)
				:serviceConfig(config),
				 messageFactory(messages)
			{
				// Validate that we are using an Enumeration TYPE
				static constexpr bool is_enum = std::is_enum<TTransportID>::value;

				socket.SetEndPoint(this);
				currentRuntime = 0;
			}

			void SendNotificationInternal(AhsokaMessageHeader& messageHeader, AhsokaMessage_t messageObject = 0)
			{
				if (socket.GetIsConnected())
				{
					if (messageObject == 0)
						messageObject = new EmptyNotification();

					messageHeader.set_client_id(socket.GetClientId());
					socket.SendMessage(messageHeader.SerializeAsString(), messageObject->SerializeAsString(), false);

					messageSendCount++;
				}
				else { }
			}

		    bool ReceiveMessage(AhsokaMessageHeader *& messageHeader, AhsokaMessageType_t *& messageObject, bool isOutOfBand, int timeoutInMs = INT_MAX)
		    {
				messageReceivedCount++;

				std::string headerData;
				std::string messageData;

				// Try to Recieve a Message
				if (!socket.ReceiveMessage(headerData, messageData, isOutOfBand, timeoutInMs))
					return false;

				// Decode Request Header
				messageHeader = new AhsokaMessageHeader();
				messageHeader->ParseFromString(headerData);

				// Lookup and Decode Message
				messageObject = messageFactory.CreateMessage<AhsokaMessageType_t>(messageHeader->transport_id(), messageData, false);
		
				return (messageObject != 0);
			}
			
			virtual void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message) = 0;

			virtual void OnEndPointConnected() {}

			virtual void OnEndPointDisconnected() {}

	private:
			
			int messageSendCount = 0;
			int messageReceivedCount = 0;
			
			AhsokaRuntime* currentRuntime;
			AhsokaSystem::ServiceConfiguration serviceConfig;
			
	};
}
