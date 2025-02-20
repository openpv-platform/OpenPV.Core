#pragma once

#include <zmq.hpp>
#include <zmq_addon.hpp>

namespace AhsokaServiceFramework
{
    class AhsokaZMQContext
    {
        public:
        
            static zmq::context_t& GetZMQContext()
            {
                static zmq::context_t context;
                return context;
            }

        private:
		
          
    };
}