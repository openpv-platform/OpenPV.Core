#pragma once
#include <string>
#include <memory>
#include <thread>
#include "AhsokaRuntime.h"

namespace AhsokaServiceFramework
{
	class Process;
	
	class AhsokaRuntimeProcess
	{
		public:

			virtual ~AhsokaRuntimeProcess();
  			
			static std::unique_ptr<AhsokaRuntimeProcess> StartServiceExecutable(const std::string& fileName);
			
			void StartProcess(std::string externalCommandLine, std::string configFile);
			
			void EndProcess();

		private:
			
			static bool Exists(const std::string& name);
			AhsokaRuntimeProcess();
			
			std::shared_ptr<Process> process;
			std::thread* runtimeThread;
		};
}

	
