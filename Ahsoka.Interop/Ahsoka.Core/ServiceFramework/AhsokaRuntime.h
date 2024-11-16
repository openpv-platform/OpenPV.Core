#pragma once
#include <memory>
#include <string>
#include <vector>
#include <zmq.hpp>

#include "IAhsokaEndPoint.h"

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
           
			friend AhsokaRuntimeBuilder;
			AhsokaRuntime();
			
			void ExecuteRuntime(std::vector<IAhsokaEndPoint*>, AhsokaRuntimeProcess*);

			template <typename TTransportID> friend class AhsokaBaseEndPoint;
			
	};
}