#pragma once
#include <vector>
#include <string>
#include <vector>
#include <functional>
#include <string>
#include <mutex>
#include <google/protobuf/message.h>

namespace AhsokaServiceFramework
{
    class IAhsokaEndPoint;

    typedef google::protobuf::Message AhsokaMessageType_t;
    typedef google::protobuf::Message * AhsokaMessage_t;

    class AhsokaEndPoint
    {
        public:

            template <typename TEndPoint>
            TEndPoint* GetEndPoint() { return static_cast<TEndPoint*>(EndPoint.get()); }

            IAhsokaEndPoint* GetEndPoint();
    
        	~AhsokaEndPoint();

        protected:
        
            AhsokaEndPoint(IAhsokaEndPoint* endPoint);

        private:
            
            std::unique_ptr<IAhsokaEndPoint> EndPoint;
    };


    template <typename TTransportID>
    class AhsokaEndPointWithCallback : public AhsokaEndPoint
    {
        public:

            typedef std::function<void(TTransportID, AhsokaMessageType_t&)> MessageReceived_t;
            virtual void RegisterListener(MessageReceived_t callback) = 0;

        protected:

            AhsokaEndPointWithCallback(IAhsokaEndPoint* endPoint) :  AhsokaEndPoint(endPoint) {}

    };
}