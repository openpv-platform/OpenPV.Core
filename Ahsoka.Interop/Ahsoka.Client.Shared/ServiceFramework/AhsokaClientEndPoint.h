#pragma once
#include "AhsokaBaseEndPoint.h"
#include <memory>
#include <mutex>
#include <list>
#include "Services.pb.h"
#include "Configuration.pb.h"
#include "AhsokaMessagesBase.h"
#include "BlockingQueue.h"

namespace AhsokaServiceFramework
{

	class MessagePair
	{
		public:

			AhsokaMessageType_t * message = 0;
			AhsokaMessageHeader * header = 0;
	};

	template <typename TTransportID>
	class AhsokaClientEndPoint : public AhsokaBaseEndPoint<TTransportID>
	{
		public:

			typedef std::function<void(TTransportID, AhsokaMessageType_t&)> MessageReceived_t;

			void NotifyMessageReceived(MessageReceived_t callback)
			{
				notificationCallback = callback;
			}

			std::unique_ptr<EmptyNotification> SendNotificationWithResponse(TTransportID transportId, AhsokaMessage_t requestObject = 0, int timeoutInMs = INT_MAX)
			{
				return SendNotificationWithResponse<EmptyNotification>(transportId, requestObject, timeoutInMs);
			}
			
			template <class T>
			std::unique_ptr<T> SendNotificationWithResponse(TTransportID transportId, AhsokaMessage_t requestObject = 0, int timeoutInMs = INT_MAX)
			{
				AhsokaMessageHeader header;
				header.set_transport_id(transportId);
				header.set_client_message_id(messageId++);

				AhsokaBaseEndPoint<TTransportID>::SendNotificationInternal(header, requestObject);

				T* returnValue = WaitForResponse<T>(header, timeoutInMs);
				
				return std::unique_ptr<T>(returnValue);
			}
        
			
			AhsokaClientEndPoint(AhsokaSystem::ServiceConfiguration config, AhsokaMessagesBase messages)
				:AhsokaBaseEndPoint<TTransportID>(config, messages)
			{
				// Validate that we are using an Enumeration TYPE at compile time
				static constexpr bool is_enum = std::is_enum<TTransportID>::value;
			}

		protected:

			void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message)
			{
				if (notificationCallback != 0)
					notificationCallback((TTransportID)messageHeader.transport_id(), message);
			}

			void ClientDataReady()
			{
				MessagePair pair;
					
				// Recieve Out of Band Message and Process on Handler
				if (this->ReceiveMessage(pair.header, pair.message, true))
				{
					ServiceMessageInfo info = this->messageFactory.GetMessageInfo(pair.header->transport_id());
					
					if (info.IsNotification && pair.header->transport_id() >= 0)
					{
						notificationMessages.Push(pair);
					}				
					else 
					{
						incomingMessages.Push(pair);
					}
				}
			}

			void OnEndPointConnected()
			{
				UpdateClientRegistration(RegisterClient);

				if (UsingNotification())
				{
					isListening = true;
					notificationThread = new std::thread(&AhsokaClientEndPoint::Listen, this);
				}					
			}

			void OnEndPointDisconnected()
			{
				if (notificationThread != 0)
				{
					UpdateClientRegistration(DeregisterClient);

					isListening = false;
					notificationThread->join();
					notificationThread = 0;
				}
			}

		private:


			int messageId = 0;
			MessageReceived_t notificationCallback;
			bool isListening = false;
			std::thread* notificationThread = 0;
			AhsokaServiceFramework::BlockingQueue<MessagePair> incomingMessages;
			AhsokaServiceFramework::BlockingQueue<MessagePair> notificationMessages;

			
			template <class T>
			T * WaitForResponse(AhsokaMessageHeader& requestHeader, int timeoutInMs = INT_MAX)
			{
				int responseTransportId = -1;
				T * returnObject = 0;

				auto expireTime = std::chrono::steady_clock::now().time_since_epoch() + std::chrono::milliseconds(timeoutInMs);
				
				// We found our Message or Timed out Waiting for Messages
				while (responseTransportId != requestHeader.transport_id()
					&& expireTime > std::chrono::steady_clock::now().time_since_epoch())
				{
					MessagePair pair;
					if (incomingMessages.PopWithTimeOut(1000, pair))
					{
						responseTransportId = pair.header->transport_id();

						// Not our Wait Message...send to Async Handler
						if (responseTransportId != requestHeader.transport_id())
						{
							notificationMessages.Push(pair);
						}
						else 
						{
							delete pair.header;
							returnObject = (T*) pair.message;
						}
					}					
				}
					
				return returnObject;
			}

			void UpdateClientRegistration(CoreTransportIds transportId)
			{
				AhsokaMessageHeader header;
				header.set_transport_id(transportId);
				header.set_client_message_id(messageId++);

				ClientRegistration registration;
				registration.set_client_id(AhsokaBaseEndPoint<TTransportID>::socket.GetClientId());

				AhsokaBaseEndPoint<TTransportID>::SendNotificationInternal(header, &registration);
			}

			void Listen()
			{				
				while (isListening) 
				{
					MessagePair pair;
					if (notificationMessages.PopWithTimeOut(1000, pair))
					{
						OnHandleReceive(*pair.header, *pair.message);
						delete(pair.header);
						delete(pair.message);
					}				
				}			
			}

			bool UsingNotification()
			{
				for (auto const& x: this->messageFactory.GetMessageInfo())
				{
					if (x.second.IsNotification)
						return true;
				}
				return false;
			}

	};
}
