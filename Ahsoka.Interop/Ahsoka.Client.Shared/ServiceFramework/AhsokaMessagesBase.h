#pragma once
#include <typeinfo>
#include <map>
#include <functional>
#include "IAhsokaEndPoint.h"
#include "AhsokaEndPoint.h"

namespace AhsokaServiceFramework
{
    typedef std::function<AhsokaMessage_t()> MessageConstructor_t;

    class ServiceMessageInfo
    {
        public:
            int TransportId;
            MessageConstructor_t SendCTOR;
            MessageConstructor_t ReceiveCTOR;
    };

    class AhsokaMessagesBase
	{
        public:
 
            ServiceMessageInfo GetMessageInfo(int transportType)
            {
                return serviceInfo[transportType];
            }
            
            template <class T>
            T * CreateMessage(int transportType, const std::string& data, bool isSendObject)
            {
                static_assert(std::is_base_of<AhsokaMessageType_t, T>::value, "T must be a Protobuf Object Type");

                // Get Message Data
                ServiceMessageInfo info = GetMessageInfo(transportType);

                // Grab Object Constructor
                MessageConstructor_t ctor = isSendObject ? info.SendCTOR : info.ReceiveCTOR;
                if (ctor != 0)
                {
                    AhsokaMessage_t message = (AhsokaMessage_t) ctor();
                    message->ParseFromString(data);
                    return  (T*)message;
                }
                else
                    return 0;
            }

        protected:

            void RegisterServiceRequest(int transportId, std::function<AhsokaMessage_t()> sendMessage,
                                                    std::function<AhsokaMessage_t()> recieveMessage )
            {
                ServiceMessageInfo info;
                info.SendCTOR = sendMessage;
                info.ReceiveCTOR = recieveMessage;
                info.TransportId = transportId;

                serviceInfo[transportId] = info;
            }

            void RegisterServiceNotification(int transportId, std::function<AhsokaMessage_t()> notificationMessage)
            {
                ServiceMessageInfo info;
                info.SendCTOR = notificationMessage;
                info.ReceiveCTOR = notificationMessage;
                info.TransportId = transportId;

                serviceInfo[transportId] = info;
            }
                    
            template <class T>
			MessageConstructor_t CreateCTOR()
			{
                static_assert(std::is_base_of<AhsokaMessageType_t, T>::value, "T must be a Protobuf Object Type");
				return []()->AhsokaMessage_t{return new T();};
			}
         
        private:

            std::map<int,ServiceMessageInfo> serviceInfo;
	};
}

