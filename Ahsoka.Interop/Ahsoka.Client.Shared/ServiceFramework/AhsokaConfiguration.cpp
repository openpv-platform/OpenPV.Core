#include <string>
#include <fstream>
#include <sstream>
#include "AhsokaConfiguration.h"
#include <google/protobuf/util/json_util.h>

using namespace google::protobuf::util;
using namespace std;

namespace AhsokaServiceFramework
{
	#include "ServiceConfiguration.hpp"

	RuntimeConfiguration AhsokaConfiguration::runtimeConfig;

	void AhsokaConfiguration::LoadConfiguration(const string& configurationFile)
	{
		// Load File
		std::ifstream t(configurationFile);
		std::stringstream buffer;
		buffer << t.rdbuf();

		//std::cout << buffer.str() << std::endl;
		JsonStringToMessage(buffer.str(), &AhsokaConfiguration::runtimeConfig);
	}

	ServiceConfiguration AhsokaConfiguration::GetServiceConfiguration(const string& serviceName)
	{
		std::string serviceConfig(ServiceConfig, sizeof(ServiceConfig));
		
		// Load Default Configuration
		if (runtimeConfig.services_size() == 0)
		{
			auto result = JsonStringToMessage(serviceConfig, &AhsokaConfiguration::runtimeConfig);
		}

		// Find Service Configuration;
		ServiceConfiguration configuration;
		for (auto config : runtimeConfig.services())
		{
			if (config.service_name() == serviceName)
				configuration = config;
		}

		return configuration;
	}
}
