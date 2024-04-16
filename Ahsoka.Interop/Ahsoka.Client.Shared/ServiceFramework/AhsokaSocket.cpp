#include "AhsokaSocket.h"

namespace AhsokaServiceFramework
{
    std::string clientId;
    zmq::context_t *context;
    
    bool IsConnected = false;
    bool IsListening = false;

    zmq::socket_t* socket;
    zmq::socket_t* notificationSocket;
    IAhsokaEndPoint* endPoint;
    std::thread* listenerThread = 0;

    std::string AhsokaSocket::dataChannel() {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_connection_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().data_channel());
    }
    std::string AhsokaSocket::dataListenChannel() {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_listen_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().data_channel());
    }
    std::string AhsokaSocket::notificationChannel() {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_connection_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().notification_channel());
    }
    std::string AhsokaSocket::notificationListenChannel() {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_listen_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().notification_channel());
    }

    bool AhsokaSocket::GetIsConnected() { return IsConnected; }

    void AhsokaSocket::SetEndPoint(IAhsokaEndPoint* endpoint) 
    { 
        this->endPoint = endpoint; 
    }

    void AhsokaSocket::Connect(zmq::context_t& context)
    {
        try
        {
            clientId = CreateGUID();
            this->context = &context;

            socket = new zmq::socket_t(GetContext(), zmq::socket_type::dealer);
            socket->set(zmq::sockopt::routing_id, clientId.c_str());

            socket->connect(dataChannel());

            std::int32_t notificationIdentifier = endPoint->GetRuntimeConfiguration().notification_channel();
            if (notificationIdentifier != 0)
                    listenerThread = new std::thread(&AhsokaSocket::ManageSocket, this);
            
            IsConnected = true;
        }
        catch (...)
        {
            IsConnected = false;
        }
    }

    void AhsokaSocket::Disconnect()
    {
        socket->disconnect(dataChannel());
        socket->close();
        
        if (listenerThread != 0)
        {
            IsListening = false;
            listenerThread->join();
            listenerThread = 0;
        }
        
        IsConnected = false;
    }

    void AhsokaSocket::SendMessage(const std::string& header, const std::string& data, bool isOutOfBand)
    {
        zmq::socket_t* socketToHandle = isOutOfBand ? notificationSocket : socket;

        zmq::multipart_t message;
        message.add(zmq::message_t(header));
        message.add(zmq::message_t(data));
        message.send(*socketToHandle, 0);
    }

    bool AhsokaSocket::ReceiveMessage(std::string& header, std::string& data, bool isOutOfBand, int timeoutInMs)
    {
        zmq::socket_t* socketToHandle = isOutOfBand ? notificationSocket : socket;

        // Receive message
        try
        {
            // Set Timeout
            socketToHandle->set(zmq::sockopt::rcvtimeo, timeoutInMs);
            
            // Receive Data
            zmq::multipart_t received;
            if (received.recv(*socketToHandle, 0))
            {
                header = received.popstr();
                data = received.popstr();
                return true;
            };
        }
        catch(...)
        {
            return false;
        }

        return false;
    }

    std::string AhsokaSocket::GetClientId() { return clientId; }

    zmq::context_t& AhsokaSocket::GetContext()
    {
        return *context;
    }

    void AhsokaSocket::ManageSocket()
    {
        std::cout << "Notification Listener Started" << std::endl;

        notificationSocket = new zmq::socket_t(GetContext(), zmq::socket_type::sub);
        notificationSocket->connect(notificationChannel());
        notificationSocket->set(zmq::sockopt::subscribe, "");
        
        // Create "array" of items to Poll...Note that ZMQ requires you to cast to Void*
        zmq::pollitem_t items[] = { { static_cast<void*>(*notificationSocket), 0, ZMQ_POLLIN, 0 } };

        IsListening = true;
        while (IsListening)
        {
            int value = zmq::poll(items, 1, 100);
            if (value != 0)
                endPoint->NotificationDataReady();
        }

        
        notificationSocket->disconnect(notificationChannel());
        notificationSocket->close();
        
        std::cout << "Notification Listener Shutdown Successfully" << std::endl;
    }

    
    std::string AhsokaSocket::CreateGUID()
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