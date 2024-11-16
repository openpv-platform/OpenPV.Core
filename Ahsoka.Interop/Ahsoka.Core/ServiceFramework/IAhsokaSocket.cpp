#include "IAhsokaSocket.h"

namespace AhsokaServiceFramework
{
    std::string IAhsokaSocket::GetDataChannel() 
    {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_connection_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().data_channel());
    }
    std::string IAhsokaSocket::GetDataListenChannel() 
    {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_listen_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().data_channel());
    }

    std::string IAhsokaSocket::CreateGUID()
    {
        std::string uuid;
        uuid.resize(16);
        std::random_device rd;
        std::uniform_int_distribution<int> dist(0, 255);

        for (auto i = 0; i < uuid.size(); i++)
            uuid[i] = (char)dist(rd);

        return uuid;
    }


   
}