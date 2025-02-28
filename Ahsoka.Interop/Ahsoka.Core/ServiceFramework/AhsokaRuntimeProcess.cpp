#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include "process.hpp"
#include "AhsokaRuntimeProcess.h"


namespace AhsokaServiceFramework
{
    AhsokaRuntimeProcess::AhsokaRuntimeProcess()
	{

	}

	AhsokaRuntimeProcess::~AhsokaRuntimeProcess()
	{

	}

	std::unique_ptr<AhsokaRuntimeProcess> AhsokaRuntimeProcess::StartServiceExecutable(const std::string& fileName)
	{
		std::string autoStartFileName = ".appinfo/AutoStartEnabled";
		if (Exists(autoStartFileName))
		{	
			std::cout << std::endl << "Skipping External Service Startup since Autostart Configuration was found" << std::endl;
			return 0;
		}
		
		if (!Exists(fileName))
			throw std::logic_error("Service Executable not specified. Use SetServiceExecutable to reference an application that accepts --StartServices <RuntimeConfigurationFile> as parameters.");
		
		std::string configFile = ".appinfo/AutoStartServiceConfig.pbuff";
		
		std::cout << std::endl << "Process Starting at " << fileName << " --StartServices " << configFile << std::endl;

		AhsokaRuntimeProcess* process = new AhsokaRuntimeProcess();
		process->StartProcess(fileName, configFile);

		return std::unique_ptr<AhsokaRuntimeProcess>(process);
	}

	void AhsokaRuntimeProcess::StartProcess(std::string externalCommandLine, std::string configFile)
	{
		// Give Permissions to Command Line Executable
		std::cout <<  "Granting Execute chmod +x " << externalCommandLine << std::endl;
		std::string chmodCommand = "chmod +x " + externalCommandLine;
		system(chmodCommand.c_str());

		// Start Process for Services and Listen to COUT
		auto serviceProcess = std::make_shared<Process>(externalCommandLine + " --StartServices " + configFile, "", [](const char* bytes, size_t n)
		{
			std::cout << std::string(bytes, n);
		});

		process = serviceProcess;
		runtimeThread = new std::thread([serviceProcess, configFile]()
		{
			auto exit_status = serviceProcess->get_exit_status();
			std::cout << "Service Process Ended" << std::endl;
		});
	}

	void AhsokaRuntimeProcess::EndProcess()
	{
		process->kill(true);
		runtimeThread->join();
	}

    bool AhsokaRuntimeProcess::Exists(const std::string& name)
	{
		struct stat buffer;
		return (stat(name.c_str(), &buffer) == 0);
	}

	

	
}