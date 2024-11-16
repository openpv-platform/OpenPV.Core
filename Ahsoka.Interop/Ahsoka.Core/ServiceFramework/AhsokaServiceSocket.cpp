#include "AhsokaServiceSocket.h"
#include "AhsokaZMQContext.h"
#include "Services.pb.h"

namespace AhsokaServiceFramework
{
    bool AhsokaServiceSocket::GetIsConnected() { return IsConnected; }

    void AhsokaServiceSocket::SetEndPoint(IAhsokaEndPoint* endpoint) 
    { 
        this->endPoint = endpoint; 
    }

    std::string AhsokaServiceSocket::GetPairAddress() 
    {
        return "inproc://" + endPoint->GetRuntimeConfiguration().service_name();
    }

    void AhsokaServiceSocket::Connect()
    {
        try
        {
            socketThread = new std::thread(&AhsokaServiceSocket::ManageSocket, this);
            socketThreadReady.WaitForReady();

            notificationThread = new std::thread(&AhsokaServiceSocket::ManageNotifications, this);
            notificationThreadReady.WaitForReady();

            IsConnected = true;
        }
        catch (...)
        {
            IsConnected = false;
        }
    }

    void AhsokaServiceSocket::Disconnect()
    {
        if (socketThread != 0)
        {
            IsListening = false;
            socketThread->join();
            socketThread = 0;
        }

        if (notificationThread != 0)
        {
            IsNotifying = false;
            notificationThread->join();
            notificationThread = 0;
        }
     
        IsConnected = false;
    }

    void  AhsokaServiceSocket::RegisterClient(ClientRegistration* registration)
    {
        clientList.push_back(registration->client_id());
    }

    void  AhsokaServiceSocket::DeregisterClient(ClientRegistration* registration)
    {
        auto itr = std::find(clientList.begin(), clientList.end(), registration->client_id());
        if (itr != clientList.end()) 
            clientList.erase(itr);
    }

    void AhsokaServiceSocket::SendMessage(std::string header, std::string data, std::string clientId, bool sendAsNotification)
    {
        if (sendAsNotification)
        {
            SocketMessage message;
            message.header = header;
            message.message = data;
            message.isNotification = true;
            notificationMessages.Push(message);
        }
        else
        {
            zmq::multipart_t message;
            message.add(zmq::message_t(clientId));
            message.add(zmq::message_t(header));
            message.add(zmq::message_t(data));
            message.send(*socket, 0);
        }
    }

    bool AhsokaServiceSocket::ReceiveMessage(std::string& header, std::string& data,  std::string& clientId, bool isNotification, int timeoutInMs)
    {
        // Receive message
        try
        {
            // Set Timeout
            socket->set(zmq::sockopt::rcvtimeo, timeoutInMs);
            
            // Receive Data
            zmq::multipart_t received;
            if (received.recv(*socket, 0))
            {               
                clientId = received.popstr();
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

   
    zmq::context_t& AhsokaServiceSocket::GetContext()
    {
        return *context;
    }

    void AhsokaServiceSocket::ManageNotifications()
    {
        zmq::socket_t* notif_socket_snd;
    
        notif_socket_snd = new zmq::socket_t(GetContext(), zmq::socket_type::pair);
        notif_socket_snd->set(zmq::sockopt::linger, 0);
        notif_socket_snd->connect(GetPairAddress());

        std::cout << "Service Notification Listener Started" << std::endl;

        IsNotifying = true;
        notificationThreadReady.SetReady();

        while (IsNotifying)
        {
            SocketMessage messageInfo;
            if(notificationMessages.PopWithTimeOut(1000, messageInfo))
            {
               // We route the notifications onto the 
                zmq::multipart_t message;
                message.add(zmq::message_t(messageInfo.header));
                message.add(zmq::message_t(messageInfo.message));
                message.send(*notif_socket_snd, 0);
            }
        }

         // Notification Sender
        notif_socket_snd->unbind(GetPairAddress());
        notif_socket_snd->close();

        std::cout << "Service Notification Listener" << std::endl;
    }

    void AhsokaServiceSocket::ManageSocket()
    {
        this->context = &AhsokaZMQContext::GetZMQContext();

        zmq::socket_t* notif_socket_rcv;
        notif_socket_rcv = new zmq::socket_t(GetContext(), zmq::socket_type::pair);
        notif_socket_rcv->set(zmq::sockopt::linger, 0);
        notif_socket_rcv->bind(GetPairAddress());
        
        // Start Message Socket
        socket = new zmq::socket_t(GetContext(), zmq::socket_type::router);
        socket->set(zmq::sockopt::linger, 0);
        socket->bind(GetDataListenChannel());
        
        std::cout << "Service Listener Started" << std::endl;
        
        // Create "array" of items to Poll...Note that ZMQ requires you to cast to Void*
        // Listen to Notification Socket
        zmq::pollitem_t items[] = 
        { 
            { static_cast<void*>(*socket), 0, ZMQ_POLLIN, 0 } ,
            { static_cast<void*>(*notif_socket_rcv), 0, ZMQ_POLLIN, 0 } ,
        };

        IsListening = true;
        socketThreadReady.SetReady();

        while (IsListening)
        {
            int value = zmq::poll(items, 2, 1000);
            if (value != 0)
            {
                if (items[0].revents & ZMQ_POLLIN) 
                {
                    // Recieve Standard Messages
                    endPoint->RecieveMessage(false);
                }
                else if (items[1].revents & ZMQ_POLLIN) 
                {
                    // Receive Data
                    zmq::multipart_t received;
                    if (received.recv(*notif_socket_rcv, 0))
                    {               
                         std::string header = received.popstr();
                         std::string data = received.popstr();
                        
                        // Send to All Clients
                        for (auto it: clientList)
                        {
                            zmq::multipart_t message;
                            message.add(zmq::message_t(it));
                            message.add(zmq::message_t(header));
                            message.add(zmq::message_t(data));
                            message.send(*socket, 0);
                        }
                    }
                }
            }
        }        

        /* Obtain real endpoint Must Get "Real" Endpoint for Unbind 
           since the '*' will not match the "Bind" string */
        const std::string last_endpoint = socket->get(zmq::sockopt::last_endpoint);
        socket->unbind(last_endpoint);
        socket->close();

        notif_socket_rcv->unbind(GetPairAddress());
        notif_socket_rcv->close();

        std::cout << "Service Listener Shutdown Successfully" << std::endl;
    }
}