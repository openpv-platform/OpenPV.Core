#pragma once
#include <memory>
#include <string>
#include <vector>
#include <zmq.hpp>

#include "AhsokaEndPoint.h"

namespace AhsokaServiceFramework
{
	template <typename TTransportID> class AhsokaBaseEndPoint;
	class AhsokaRuntimeBuilder;
	class AhsokaRuntimeProcess;

	class AhsokaRuntime
	{
		public:

			static AhsokaRuntimeBuilder CreateBuilder();
			static void ShutdownAll();

			~AhsokaRuntime();
  			AhsokaRuntime(const AhsokaRuntime& other);

			static void ReleaseStartup();
			void RequestShutdown();
			void WaitForShutdown();
	
		private:

			class Details;
			std::unique_ptr<Details> details;
            zmq::context_t context;
			
			friend AhsokaRuntimeBuilder;
			AhsokaRuntime();
			
			void ExecuteRuntime(std::vector<AhsokaEndPoint*>, AhsokaRuntimeProcess*);

			template <typename TTransportID> friend class AhsokaBaseEndPoint;
			zmq::context_t& GetZMQContext()
			{
				return context;
			}
	};
}