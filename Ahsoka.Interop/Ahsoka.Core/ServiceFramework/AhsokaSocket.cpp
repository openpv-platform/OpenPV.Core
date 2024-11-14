#include "AhsokaSocket.h"

namespace AhsokaServiceFramework
{
    std::string clientId;
    zmq::context_t *context;
    
    bool IsConnected = false;
    bool IsListening = false;
    bool IsSending = false;

    zmq::socket_t* socket;
    IAhsokaEndPoint* endPoint;
    std::thread* listenerThread = 0;
    std::thread* senderThread = 0;

    std::string AhsokaSocket::dataChannel() {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_connection_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().data_channel());
    }
    std::string AhsokaSocket::dataListenChannel() {
        return "tcp://" + endPoint->GetRuntimeConfiguration().tcp_listen_address() + ":" + std::to_string(endPoint->GetRuntimeConfiguration().data_channel());
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
            notificationClientId = CreateGUID();

            this->context = &context;

            // Start Message Socket
            socket = new zmq::socket_t(GetContext(), zmq::socket_type::dealer);
            socket->set(zmq::sockopt::routing_id, clientId.c_str());
            socket->connect(dataChannel());

            // Start Notification Socket
            notificationSocket = new zmq::socket_t(GetContext(), zmq::socket_type::dealer);
            notificationSocket->set(zmq::sockopt::routing_id, notificationClientId.c_str());
            notificationSocket->connect(dataChannel());

            listenerThread = new std::thread(&AhsokaSocket::ManageListener, this);
            senderThread = new std::thread(&AhsokaSocket::ManageSender, this);
            
            IsConnected = true;
        }
        catch (...)
        {
            IsConnected = false;
        }
    }

    void AhsokaSocket::Disconnect()
    {
        if (listenerThread != 0)
        {
            IsListening = false;
            listenerThread->join();
            listenerThread = 0;
        }

        if (senderThread != 0)
        {
            IsSending = false;
            senderThread->join();
            senderThread = 0;
        }

        socket->disconnect(dataChannel());
        socket->close();
              
        IsConnected = false;
    }

    void AhsokaSocket::SendMessage(std::string header, std::string data, bool expectResponse)
    {
        SocketMessage message;
        message.header = header;
        message.message = data;
        message.expectResponse = expectResponse;
        outgoingMessages.Push(message);
    }

    bool AhsokaSocket::ReceiveMessage(std::string& header, std::string& data, bool isOutOfBand, int timeoutInMs)
    {
        // Receive message
        try
        {
             zmq::socket_t* socketToHandle = isOutOfBand ? notificationSocket : socket;

            // Set Timeout
            socketToHandle->set(zmq::sockopt::rcvtimeo, timeoutInMs);
            
            // Receive Data
            zmq::multipart_t received;
            if (received.recv(*socketToHandle, 0))
            {               
                header = received.popstr();
                data = received.popstr();
                return true;
            }
        }
        catch(...)
        {
            return false;
        }

        return false;
    }

    std::string AhsokaSocket::GetClientId() { return clientId; }
    std::string AhsokaSocket::GetNotificationClientId() { return notificationClientId; }

    zmq::context_t& AhsokaSocket::GetContext()
    {
        return *context;
    }

    void AhsokaSocket::SendMessage(const std::string& header, const std::string& data)
    {
        zmq::multipart_t message;
        message.add(zmq::message_t(header));
        message.add(zmq::message_t(data));
        message.send(*socket, 0);
    }

    void AhsokaSocket::ManageSender()
    {
        std::cout << "Client Sender Started" << std::endl;

        IsSending = true;
        while (IsSending)
        {
            SocketMessage message;
            if(outgoingMessages.PopWithTimeOut(1000, message))
            {
                SendMessage(message.header, message.message);

                if(message.expectResponse)
                    endPoint->ClientDataReady(false);          
            }
        }

        std::cout << "Client Sender Shutdown Successfully" << std::endl;
    }

    void AhsokaSocket::ManageListener()
    {
        std::cout << "Client Listener Started" << std::endl;
        
        // Create "array" of items to Poll...Note that ZMQ requires you to cast to Void*
        // Listen to Notification Socket
        zmq::pollitem_t items[] = 
        { 
            { static_cast<void*>(*notificationSocket), 0, ZMQ_POLLIN, 0 } ,
        };

        IsListening = true;
        while (IsListening)
        {
            int value = zmq::poll(items, 1, 1000);
            if (value != 0)
            {
                // Notificaiton Received
                if (items [0].revents & ZMQ_POLLIN) {
                    endPoint->ClientDataReady(true);
                }
            }
        }
        
        std::cout << "Client Listener Shutdown Successfully" << std::endl;
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