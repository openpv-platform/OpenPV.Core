#include "AhsokaRuntimeBuilder.h"
#include "AhsokaRuntime.h"
#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#include <filesystem>
#include <sys/stat.h>
#include "Configuration.pb.h"
#include "IAhsokaEndPoint.h"
#include <google/protobuf/util/json_util.h>
#include "AhsokaRuntimeProcess.hpp"


namespace fs = std::filesystem;

using namespace AhsokaSystem;

namespace AhsokaServiceFramework
{
	class AhsokaRuntimeBuilder::Details
	{
		public:

			bool isClient = false;
			std::string externalCommandline = "";
			std::vector<IAhsokaEndPoint*> clients;
			std::string defaultConfigName = "AhsokaConfig.json";
			
			AhsokaRuntimeProcess* StartProcessExecutable()
			{
				std::string autoStartFileName = ".appinfo/AutoStartEnabled";
				if (Exists(autoStartFileName))
				{	
					std::cout << std::endl << "Skipping External Service Startup since Autostart Configuration was found" << std::endl;
					return 0;
				}
				
				if (!Exists(externalCommandline))
					throw std::logic_error("Service Executable not specified. Use SetServiceExecutable to reference an application that accepts --StartServices <RuntimeConfigurationFile> as parameters.");
				
				std::string configFile = ".appinfo/AutoStartServiceConfig.pbuff";
				
				std::cout << std::endl << "Process Starting at " << externalCommandline << " --StartServices " << configFile << std::endl;

				AhsokaRuntimeProcess* process = new AhsokaRuntimeProcess();
				process->StartProcess(externalCommandline, configFile, false);

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
	
	AhsokaRuntimeBuilder& AhsokaRuntimeBuilder::AddEndPoint(IAhsokaEndPoint* client)
	{
		details->clients.push_back(client);
		return *this;
	}

	AhsokaRuntimeBuilder& AhsokaRuntimeBuilder::AddClient(IAhsokaEndPoint* client)
	{
		details->clients.push_back(client);
		return *this;
	}

	AhsokaRuntimeBuilder& AhsokaRuntimeBuilder::SetServiceExecutable(const std::string& fileName)
	{
		details->externalCommandline = fileName;
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