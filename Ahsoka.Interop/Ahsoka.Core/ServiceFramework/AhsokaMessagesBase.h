#pragma once
#include <typeinfo>
#include <map>
#include <functional>
#include "IAhsokaEndPoint.h"
#include "AhsokaBaseEndPoint.h"
#include "Services.pb.h"

namespace AhsokaServiceFramework
{
 
	typedef google::protobuf::Message AhsokaMessageType_t;
    typedef google::protobuf::Message * AhsokaMessage_t;
    typedef std::function<AhsokaMessage_t()> MessageConstructor_t;

    class ServiceMessageInfo
    {
        public:
            int TransportId;
            bool IsNotification;
            MessageConstructor_t SendCTOR;
            MessageConstructor_t ReceiveCTOR;
    };

    class AhsokaMessagesBase
	{
        public:

            std::map<int,ServiceMessageInfo> GetMessageInfo()
            {
                return serviceInfo;
            }
 
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
                info.IsNotification = false;
                info.SendCTOR = sendMessage;
                info.ReceiveCTOR = recieveMessage;
                info.TransportId = transportId;

                serviceInfo[transportId] = info;
            }

            void RegisterServiceNotification(int transportId, std::function<AhsokaMessage_t()> notificationMessage)
            {
                ServiceMessageInfo info;
                info.IsNotification = true;
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
         
            AhsokaMessagesBase()
            {
                RegisterServiceNotification(CoreTransportIds::RegisterClient, CreateCTOR<AhsokaServiceFramework::ClientRegistration>());
		        RegisterServiceNotification(CoreTransportIds::DeregisterClient, CreateCTOR<AhsokaServiceFramework::ClientRegistration>());
            }


        private:

            std::map<int,ServiceMessageInfo> serviceInfo;
	};
}

