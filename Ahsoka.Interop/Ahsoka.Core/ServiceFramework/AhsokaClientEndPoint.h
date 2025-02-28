#pragma once
#include "AhsokaBaseEndPoint.h"
#include <memory>
#include <mutex>
#include <list>
#include "Services.pb.h"
#include "Configuration.pb.h"
#include "AhsokaMessagesBase.h"
#include "BlockingQueue.h"
#include "AhsokaClientSocket.h"

namespace AhsokaServiceFramework
{
	class ClientMessagePair
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
			
			std::unique_ptr<EmptyNotification> SendMessageWithResponse(TTransportID transportId, AhsokaMessage_t requestObject = 0, int timeoutInMs = INT_MAX)
			{
				return SendMessageWithResponse<EmptyNotification>(transportId, requestObject, timeoutInMs);
			}
			
			template <class T>
			std::unique_ptr<T> SendMessageWithResponse(TTransportID transportId, AhsokaMessage_t requestObject = 0, int timeoutInMs = INT_MAX)
			{
				AhsokaMessageHeader header;
				header.set_transport_id(transportId);
				header.set_client_message_id(messageId++);
				header.set_client_id(((AhsokaClientSocket*)AhsokaBaseEndPoint<TTransportID>::socket)->GetClientId());
					
				AhsokaBaseEndPoint<TTransportID>::SendMessageInternal(header, requestObject);

				T* returnValue = WaitForResponse<T>(header, timeoutInMs);
				
				return std::unique_ptr<T>(returnValue);
			}
        
			
			AhsokaClientEndPoint(AhsokaCore::ServiceConfiguration config, AhsokaMessagesBase messages)
				:AhsokaBaseEndPoint<TTransportID>(config, messages)
			{
				// Validate that we are using an Enumeration TYPE at compile time
				static constexpr bool is_enum = std::is_enum<TTransportID>::value;
				AhsokaBaseEndPoint<TTransportID>::isServiceEndPoint = false;
				AhsokaBaseEndPoint<TTransportID>::socket = CreateSocket(config);
			}

		protected:

		    IAhsokaSocket* CreateSocket(AhsokaCore::ServiceConfiguration& config)
			{
				AhsokaClientSocket * retVal = new AhsokaClientSocket();
				retVal->SetEndPoint(this);
				return retVal;
			};

			void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message)
			{
				if (notificationCallback != 0)
					notificationCallback((TTransportID)messageHeader.transport_id(), message);
			}

			void RecieveMessage(bool isNotification)
			{
				ClientMessagePair pair;
					
				// Recieve Out of Band Message and Process on Handler
				if (this->ReceiveMessage(pair.header, pair.message, isNotification))
				{
					ServiceMessageInfo info = this->messageFactory.GetMessageInfo(pair.header->transport_id());
					
					if (info.IsNotification && pair.header->transport_id() >= 0)
					{
						// Handle Notifications Directly
						OnHandleReceive(*pair.header, *pair.message);
						delete(pair.header);
						delete(pair.message);
					}				
					else 
					{
						incomingMessages.Push(pair);
					}
				}
			}

			void OnEndPointConnected()
			{
				UpdateClientRegistration(REGISTER_CLIENT);
			}

			void OnEndPointDisconnected()
			{
				UpdateClientRegistration(DEREGISTER_CLIENT);
			}

		private:


			int messageId = 0;
			MessageReceived_t notificationCallback;
			bool isListening = false;
			AhsokaServiceFramework::BlockingQueue<ClientMessagePair> incomingMessages;
			
			template <class T>
			T * WaitForResponse(AhsokaMessageHeader& requestHeader, int timeoutInMs = INT_MAX)
			{
				int responseTransportId = -1;
				int responseClientId = -1;
				T * returnObject = 0;

				auto expireTime = std::chrono::steady_clock::now().time_since_epoch() + std::chrono::milliseconds(timeoutInMs);
				
				// We found our Message or Timed out Waiting for Messages
				while (responseTransportId != requestHeader.transport_id()
					&& responseClientId != requestHeader.client_message_id()
					&& expireTime > std::chrono::steady_clock::now().time_since_epoch())
				{
					ClientMessagePair pair;
					if (incomingMessages.PopWithTimeOut(1000, pair))
					{
						responseTransportId = pair.header->transport_id();
						responseClientId = pair.header->client_message_id();

						if (responseTransportId != requestHeader.transport_id() && responseTransportId > 0)
						{
							// Not our Wait Message...Handle Now (Is a Notification)
							OnHandleReceive(*pair.header, *pair.message);
							delete(pair.header);
							delete(pair.message);
						}
						else 
						{
							if (responseTransportId == INVALID_TOKEN)
							{
								delete(pair.header);
								delete(pair.message);
								throw "The Service returned an InvalidToken Message. This usually indicates that this security enabled endpoint was not unlocked with a valid token";
							}
							if (responseTransportId == SERVICE_ERROR_THROWN)
							{
								ServiceErrorThrownResponse* error = (ServiceErrorThrownResponse*)pair.message;
								std::string errorText =  "The Service " + error->service_name() + " returned an error. " + error->error_information();
								delete(pair.header);
								delete(pair.message);
								throw errorText;
							}

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
				header.set_client_id(((AhsokaClientSocket*)AhsokaBaseEndPoint<TTransportID>::socket)->GetClientId());
				
				ClientRegistration registration;
				registration.set_client_id(((AhsokaClientSocket*)AhsokaBaseEndPoint<TTransportID>::socket)->GetNotificationClientId()); // Note use of Notification ClientID

				AhsokaBaseEndPoint<TTransportID>::SendMessageInternal(header, &registration, true);
			}
	};
}
