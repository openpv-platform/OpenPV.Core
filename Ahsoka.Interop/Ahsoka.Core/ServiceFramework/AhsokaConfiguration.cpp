#include <string>
#include <fstream>
#include <filesystem>
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

	ServiceConfiguration AhsokaConfiguration::GetServiceConfiguration(const string& serviceName, bool throwIfNotFound)
	{
		// Load Default Configuration
		string path = ".appinfo/AutoStartServiceConfig.pbuff";
		if (runtimeConfig.services_size() == 0 && std::filesystem::exists(path))
		{
			LoadConfiguration(path);
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
		{
			if(throwIfNotFound)
				throw std::logic_error(("Service Config not found for %s", serviceName));
		
			configuration.set_service_name("");
			configuration.set_data_channel(-1);
			
			std::cout << "Service Config not found for " << serviceName << std::endl;
		}		
		return configuration;
	}

}
