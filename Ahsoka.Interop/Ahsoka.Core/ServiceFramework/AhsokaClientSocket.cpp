#include "AhsokaClientSocket.h"
#include "AhsokaZMQContext.h"

namespace AhsokaServiceFramework
{
    bool AhsokaClientSocket::GetIsConnected() { return IsConnected; }
    std::string AhsokaClientSocket::GetClientId() { return clientId; }
    std::string AhsokaClientSocket::GetNotificationClientId() { return notificationClientId; }

    void AhsokaClientSocket::SetEndPoint(IAhsokaEndPoint* endpoint) 
    { 
        this->endPoint = endpoint; 
    }

    void AhsokaClientSocket::Connect()
    {
        try
        {
            clientId = CreateGUID();
            notificationClientId = CreateGUID();

            context = &AhsokaZMQContext::GetZMQContext();

            notificationThread = new std::thread(&AhsokaClientSocket::ManageNotifications, this);
            notificationThreadReady.WaitForReady();
            
            socketThread = new std::thread(&AhsokaClientSocket::ManageSocket, this);
            socketThreadReady.WaitForReady();
            
            IsConnected = true;
        }
        catch (...)
        {
            IsConnected = false;
        }
    }

    void AhsokaClientSocket::Disconnect()
    {
        if (socketThread != 0)
        {
            IsListening = false;
            socketThread->join();
            socketThread = 0;
        }

        if (notificationThread != 0)
        {
            IsSending = false;
            notificationThread->join();
            notificationThread = 0;
        }

        IsConnected = false;
    }

    void AhsokaClientSocket::SendMessage(std::string header, std::string data, std::string clientId, bool isNotification)
    {
        SocketMessage message;
        message.header = header;
        message.message = data;
        message.isNotification = isNotification;
        outgoingMessages.Push(message);
    }

    bool AhsokaClientSocket::ReceiveMessage(std::string& header, std::string& data, 
            std::string& clientId, bool isOutOfBand, int timeoutInMs)
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

    zmq::context_t& AhsokaClientSocket::GetContext()
    {
        return *context;
    }

    void AhsokaClientSocket::SendMessage(const std::string& header, const std::string& data)
    {
        zmq::multipart_t message;
        message.add(zmq::message_t(header));
        message.add(zmq::message_t(data));
        message.send(*socket, 0);
    }

    void AhsokaClientSocket::ManageNotifications()
    {
        // Start Notification Socket
        notificationSocket = new zmq::socket_t(GetContext(), zmq::socket_type::dealer);
        notificationSocket->set(zmq::sockopt::routing_id, notificationClientId.c_str());
        notificationSocket->set(zmq::sockopt::linger, 0);
        notificationSocket->connect(GetDataChannel());
        
        std::cout << "Client Sender Started" << std::endl;

        IsSending = true;
        notificationThreadReady.SetReady();

        while (IsSending)
        {
            SocketMessage message;
            if(outgoingMessages.PopWithTimeOut(1000, message))
            {
                SendMessage(message.header, message.message);

                if(message.isNotification)
                    endPoint->RecieveMessage(false);          
            }
        }

        notificationSocket->disconnect(GetDataChannel());
        notificationSocket->close();
   
        std::cout << "Client Sender Shutdown Successfully" << std::endl;
    }

    void AhsokaClientSocket::ManageSocket()
    {
        // Start Message Socket
        socket = new zmq::socket_t(GetContext(), zmq::socket_type::dealer);
        socket->set(zmq::sockopt::routing_id, clientId.c_str());
        socket->set(zmq::sockopt::linger, 0);
        socket->connect(GetDataChannel());

        std::cout << "Client Listener Started" << std::endl;
        
        // Create "array" of items to Poll...Note that ZMQ requires you to cast to Void*
        // Listen to Notification Socket
        zmq::pollitem_t items[] = 
        { 
            { static_cast<void*>(*notificationSocket), 0, ZMQ_POLLIN, 0 } ,
        };

        IsListening = true;
        socketThreadReady.SetReady();
        while (IsListening)
        {
            int value = zmq::poll(items, 1, 1000);
            if (value != 0)
            {
                // Notificaiton Received
                if (items [0].revents & ZMQ_POLLIN) {
                    endPoint->RecieveMessage(true);
                }
            }
        }
        

        socket->disconnect(GetDataChannel());
        socket->close();
        
        std::cout << "Client Listener Shutdown Successfully" << std::endl;
    }

   
}