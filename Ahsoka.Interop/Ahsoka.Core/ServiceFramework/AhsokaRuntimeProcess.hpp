#pragma once
#include "process.hpp"

using namespace TinyProcessLib;

namespace AhsokaServiceFramework
{
	class AhsokaRuntimeProcess
	{
		public:

			void StartProcess(std::string externalCommandLine, std::string configFile, bool deleteConfigOnExit)
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
				runtimeThread = new std::thread([serviceProcess, configFile, deleteConfigOnExit]()
				{
					auto exit_status = serviceProcess->get_exit_status();

					std::cout << "Service Process Ended" << std::endl;

					if (deleteConfigOnExit)
						std::remove(configFile.c_str());
				});
			}
			
			void EndProcess()
			{
				process->kill(true);
				runtimeThread->join();
			}
		
		private:

			std::shared_ptr<TinyProcessLib::Process> process;
			std::thread* runtimeThread;
	};
}