#pragma once
#include <string>
#include <memory>
#include "AhsokaRuntime.h"

namespace AhsokaServiceFramework
{
	class AhsokaRuntimeBuilder 
	{
		public:
           
			AhsokaRuntimeBuilder();
			~AhsokaRuntimeBuilder();
  			AhsokaRuntimeBuilder(const AhsokaRuntimeBuilder& other);

			[[deprecated("Use AddEndPoint() instead.")]]
			AhsokaRuntimeBuilder& AddClient(IAhsokaEndPoint* client);
			AhsokaRuntimeBuilder& AddEndPoint(IAhsokaEndPoint* client);
			AhsokaRuntimeBuilder& SetServiceExecutable(const std::string& fileName);
			AhsokaRuntimeBuilder& SetServiceConfiguration(const std::string& fileName);
			
			AhsokaRuntime& StartLocalRuntime();
			AhsokaRuntime& StartWithExternalServices();
	    
		private:

			class Details;
			std::unique_ptr<Details> details;
	};
}

	
