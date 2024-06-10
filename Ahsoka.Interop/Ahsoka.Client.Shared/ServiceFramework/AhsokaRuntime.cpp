#include <string>
#include <fstream>
#include <sstream>
#include "IAhsokaEndPoint.hpp"
#include "AhsokaRuntimeProcess.hpp"
#include "AhsokaRuntime.h"
#include "AhsokaRuntimeBuilder.h"

using namespace AhsokaSystem;
using namespace std;

namespace AhsokaServiceFramework
{
    typedef std::lock_guard<std::mutex> AhsokaMutexLock_t;

	class AhsokaRuntime::Details
	{
		public:
			
			std::map<std::string, AhsokaEndPoint*> endPoints;
			AhsokaRuntimeProcess* process;

			static std::mutex RuntimeMutex;
			static std::vector<AhsokaRuntime*> Runtimes;
					
			void ExecuteRuntimeInternal()
			{
				for (auto it : endPoints)
					it.second->GetEndPoint()->Connect();
			}
	};

	std::mutex AhsokaRuntime::Details::RuntimeMutex;
	std::vector<AhsokaRuntime*> AhsokaRuntime::Details::Runtimes;
	
	AhsokaRuntime::AhsokaRuntime() : details(new AhsokaRuntime::Details()) { }
	AhsokaRuntime::~AhsokaRuntime() = default;
	
	AhsokaRuntime::AhsokaRuntime(const AhsokaRuntime& other)
		: details(new Details(*other.details))
	{
		
	}
 
	AhsokaRuntimeBuilder AhsokaRuntime::CreateBuilder()
	{
		AhsokaRuntimeBuilder builder;
		return builder;
	}

	void AhsokaRuntime::ExecuteRuntime(std::vector<AhsokaEndPoint*> clients, AhsokaRuntimeProcess* process)
	{
		// Add to Runtime List
		AhsokaMutexLock_t lock(Details::RuntimeMutex);
		Details::Runtimes.push_back(this);

		// Create Context Items.
		for (auto it : clients)
		{
			it->GetEndPoint()->SetRuntime(this);
			details->endPoints[it->GetEndPoint()->GetRuntimeConfiguration().service_name()] = it;
		}

		// Capture for Shutdown
		details->process = process;

		// Start Services or Clients
		details->ExecuteRuntimeInternal();
	}

	void AhsokaRuntime::ReleaseStartup()
	{
		system("systemd-notify --ready");
	}

	void  AhsokaRuntime::RequestShutdown()
	{
		for (auto it : details->endPoints)
			it.second->GetEndPoint()->Disconnect();

		if (details->process != 0)
			details->process->EndProcess();
	}

	void AhsokaRuntime::WaitForShutdown()
	{
		AhsokaMutexLock_t lock(Details::RuntimeMutex);
		Details::Runtimes.erase(std::remove(Details::Runtimes.begin(), Details::Runtimes.end(), this), Details::Runtimes.end());
	}

	void AhsokaRuntime::ShutdownAll()
	{
		for (auto it : Details::Runtimes)
		{
			it->RequestShutdown();
			it->WaitForShutdown();
		}
	}
}
