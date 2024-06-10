#include "AhsokaRuntimeBuilder.h"
#include "AhsokaRuntime.h"
#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#include <sys/stat.h>
#include "Configuration.pb.h"
#include "IAhsokaEndPoint.hpp"
#include <google/protobuf/util/json_util.h>
#include "AhsokaEndPoint.h"
#include "AhsokaRuntimeProcess.hpp"
#include <filesystem>

namespace fs = std::filesystem;

using namespace AhsokaSystem;

namespace AhsokaServiceFramework
{
	class AhsokaRuntimeBuilder::Details
	{
		public:

			bool isClient = false;
			std::string externalCommandline = "";
			std::string serviceConfigurationFile = "";
			std::vector<AhsokaEndPoint*> clients;
			std::string defaultConfigName = "AhsokaConfig.json";
			
			std::string CreateServiceConfiguration(std::vector<AhsokaEndPoint*>& clients)
			{	
				RuntimeConfiguration rt;
				for (auto endpoint : clients)
				{					
					ServiceConfiguration* tempConfig = rt.add_services();
					tempConfig->CopyFrom(endpoint->GetEndPoint()->GetRuntimeConfiguration());
				}
				
				// Create Temp Configuration File
				std::string json_string;
				google::protobuf::util::JsonPrintOptions options;
				options.add_whitespace = true;
				options.always_print_primitive_fields = true;
				MessageToJsonString(rt, &json_string, options);
				
				// Create Temp File name
				std::stringstream tempName;
				tempName << std::chrono::system_clock::now().time_since_epoch().count() << "AhsokaConfig.json";

				fs::path tempFile = fs::temp_directory_path() / tempName.str();

				std::ofstream configFile(tempFile);
				configFile << json_string;
					configFile.close();

				return tempFile.string();
			}
		
			AhsokaRuntimeProcess* StartProcessExecutable()
			{
				std::string autoStartFileName = ".appinfo/AutoStartServiceConfig.json";
				if (Exists(autoStartFileName))
				{	
					std::cout << std::endl << "Skipping External Service Startup since Autostart Configuration was found" << std::endl;
					return 0;
				}
				
				if (!Exists(externalCommandline))
					throw std::logic_error("Service Executable not specified. Use SetServiceExecutable to reference an application that accepts --StartServices <RuntimeConfigurationFile> as parameters.");
				
				std::string configFile;

				if (serviceConfigurationFile == "")
					configFile = CreateServiceConfiguration(clients);  
				else
					configFile  = serviceConfigurationFile;  

				std::cout << std::endl << "Process Starting at " << externalCommandline << " --StartServices " << configFile << std::endl;

				AhsokaRuntimeProcess* process = new AhsokaRuntimeProcess();
				process->StartProcess(externalCommandline, configFile, serviceConfigurationFile == "");

				return process;
			}

			inline bool Exists(const std::string& name)
			{
				struct stat buffer;
				return (stat(name.c_str(), &buffer) == 0);
			}
	};

	AhsokaRuntimeBuilder::AhsokaRuntimeBuilder() : details(new AhsokaRuntimeBuilder::Details()) { }
	AhsokaRuntimeBuilder::~AhsokaRuntimeBuilder() = default;
	AhsokaRuntimeBuilder::AhsokaRuntimeBuilder(const AhsokaRuntimeBuilder& other)
		: details(new Details(*other.details))
	{
		
	}
	
	AhsokaRuntimeBuilder& AhsokaRuntimeBuilder::AddClient(AhsokaEndPoint* client)
	{
		details->clients.push_back(client);
		return *this;
	}

	AhsokaRuntimeBuilder& AhsokaRuntimeBuilder::SetServiceExecutable(const std::string& fileName)
	{
		details->externalCommandline = fileName;
		return *this;
	}

	AhsokaRuntimeBuilder& AhsokaRuntimeBuilder::SetServiceConfiguration(const std::string& fileName)
	{
		details->serviceConfigurationFile = fileName;
		return *this;
	}

	AhsokaRuntime& AhsokaRuntimeBuilder::StartLocalRuntime()
	{
		AhsokaRuntime* runtime = new AhsokaRuntime();
		runtime->ExecuteRuntime(details->clients, 0);
		return *runtime;
	}

	AhsokaRuntime& AhsokaRuntimeBuilder::StartWithExternalServices()
	{
		// Start Executable for Services....
		AhsokaRuntime* runtime = new AhsokaRuntime();
		auto process = details->StartProcessExecutable();
		runtime->ExecuteRuntime(details->clients, process);
		return *runtime;
	}
}