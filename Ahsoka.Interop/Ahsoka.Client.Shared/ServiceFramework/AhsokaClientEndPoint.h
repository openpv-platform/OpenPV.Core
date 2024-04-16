#pragma once
#include "AhsokaBaseEndPoint.h"
#include <memory>
#include "Services.pb.h"
#include "Configuration.pb.h"
#include "AhsokaMessagesBase.h"

namespace AhsokaServiceFramework
{
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

			void NotificationDataReady()
			{
				AhsokaMessageType_t * message = 0;
				AhsokaMessageHeader * header = 0;
					
				// Recieve Out of Band Message and Process on Handler
				if (this->ReceiveMessage(header, message, true))
				{
					OnHandleReceive(*header, *message);
					delete message;
					delete header;
				}
			}

		private:


			int messageId = 0;
			MessageReceived_t notificationCallback;
			
			template <class T>
			T * WaitForResponse(AhsokaMessageHeader& requestHeader, int timeoutInMs = INT_MAX)
			{
				int responseTransportId = -1;
				T * returnObject = 0;

				auto expireTime = std::chrono::system_clock::now().time_since_epoch() + std::chrono::milliseconds(timeoutInMs);
				
				// We found our Message or Timed out Waiting for Messages
				while (responseTransportId != requestHeader.transport_id()
					&& expireTime > std::chrono::system_clock::now().time_since_epoch())
				{
					AhsokaMessageType_t * message = 0;
					AhsokaMessageHeader * header = 0;
					
					if (this->ReceiveMessage(header, message, false, timeoutInMs))
					{
						// Get new Message ID
						responseTransportId = header->transport_id();

						// Not our Wait Message...send to Async Handler
						if (responseTransportId != requestHeader.transport_id())
						{
							OnHandleReceive(*header, *message);
							delete message;
							delete header;
						}
						else
						{  
							// Delete Header but not Message since we will be returning that
							delete header;
							returnObject = (T*) message;
						}
					}
				}
					
				return returnObject;
			}

	};
}
