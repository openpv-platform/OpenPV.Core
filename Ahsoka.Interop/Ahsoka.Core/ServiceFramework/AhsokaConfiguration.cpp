#include <string>
#include <fstream>
#include <sstream>
#include "AhsokaConfiguration.h"
#include <google/protobuf/util/json_util.h>

using namespace google::protobuf::util;
using namespace std;

namespace AhsokaServiceFramework
{

	RuntimeConfiguration AhsokaConfiguration::runtimeConfig;

	void AhsokaConfiguration::LoadConfiguration(const string& configurationFile)
	{
		std::ifstream ifs(configurationFile, std::ifstream::in);
		AhsokaConfiguration::runtimeConfig.ParseFromIstream(&ifs);
	}

	ServiceConfiguration AhsokaConfiguration::GetServiceConfiguration(const string& serviceName)
	{
		// Load Default Configuration
		if (runtimeConfig.services_size() == 0)
		{
			LoadConfiguration(".appinfo/AutoStartServiceConfig.pbuff");
		}

		// Find Service Configuration;
		bool found = false;
		ServiceConfiguration configuration;
		for (auto config : runtimeConfig.services())
		{
			if (config.service_name() == serviceName)
			{
				configuration = config;
				found = true;
			}
				
		}

		if (!found)
			throw std::logic_error(("Service Config not found for %s", serviceName));

		return configuration;
	}

}
