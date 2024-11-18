#pragma once
#include "AhsokaBaseEndPoint.h"
#include <memory>
#include <mutex>
#include <list>
#include "Services.pb.h"
#include "Configuration.pb.h"
#include "AhsokaMessagesBase.h"
#include "BlockingQueue.h"
#include "AhsokaServiceSocket.h"

namespace AhsokaServiceFramework
{

	class ServiceMessagePair
	{
		public:

			AhsokaMessageType_t * message = 0;
			AhsokaMessageHeader * header = 0;
	};

	template <typename TTransportID>
	class AhsokaServiceEndPoint : public AhsokaBaseEndPoint<TTransportID>
	{
		public:

			AhsokaServiceEndPoint(AhsokaSystem::ServiceConfiguration config, AhsokaMessagesBase messages)
				:AhsokaBaseEndPoint<TTransportID>(config, messages)
			{
				// Validate that we are using an Enumeration TYPE at compile time
				static constexpr bool is_enum = std::is_enum<TTransportID>::value;

				AhsokaBaseEndPoint<TTransportID>::socket = CreateSocket(config);
			}

		protected:

		    IAhsokaSocket* CreateSocket(AhsokaSystem::ServiceConfiguration& config)
			{
				AhsokaServiceSocket * retVal = new AhsokaServiceSocket();
				retVal->SetEndPoint(this);
				return retVal;
			};

			virtual void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message) = 0;

		    void RecieveMessage(bool isNotification)
			{
				ServiceMessagePair info;

				// Recieve Out of Band Message and Process on Handler
				if (this->ReceiveMessage(info.header, info.message, isNotification, 1000))
				{
					// Handle Global Notification Registration
					if (info.header->transport_id() == CoreTransportIds::RegisterClient)
					{
						((AhsokaServiceSocket*)AhsokaBaseEndPoint<TTransportID>::socket)->RegisterClient((ClientRegistration*)info.message);
					}
					else if (info.header->transport_id() == CoreTransportIds::DeregisterClient)
					{
						((AhsokaServiceSocket*)AhsokaBaseEndPoint<TTransportID>::socket)->DeregisterClient((ClientRegistration*)info.message);
					}
					else
					{
						OnHandleReceive(*info.header, *info.message);
					}	

					delete(info.header);
					delete(info.message);
				}
			}

		private:

	 
	};
}
