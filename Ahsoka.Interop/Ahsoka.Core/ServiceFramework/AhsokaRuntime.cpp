#include <string>
#include <fstream>
#include <sstream>
#include "IAhsokaEndPoint.h"
#include "AhsokaRuntime.h"
#include "AhsokaZMQContext.h"

using namespace AhsokaCore;
using namespace std;

namespace AhsokaServiceFramework
{
    typedef std::lock_guard<std::mutex> AhsokaMutexLock_t;
	static std::mutex RuntimeMutex;
	
    AhsokaRuntime& AhsokaRuntime::GetDefaultRuntime()
	{
		static AhsokaRuntime defaultRuntime;
		return defaultRuntime;
	}

	void AhsokaRuntime::StartEndPoints(std::vector<IAhsokaEndPoint*> endpointList)
	{
		// Create Context Items.
		for (auto it : endpointList)
			it->Start();
	}
	
	void AhsokaRuntime::StopEndPoints(std::vector<IAhsokaEndPoint*> endPointList)
	{
		for (auto it : endPointList)
			it->Stop();
	}
	
	void AhsokaRuntime::AddEndPoint(IAhsokaEndPoint* endPoint)
	{
		AhsokaMutexLock_t lock(RuntimeMutex);
		GetDefaultRuntime().endPoints[endPoint->GetRuntimeConfiguration().service_name()] = endPoint;
	}

	void AhsokaRuntime::StopAllEndPoints()
	{
		AhsokaMutexLock_t lock(RuntimeMutex);
		for (auto it : GetDefaultRuntime().endPoints)
			it.second->Stop();
	}

	void AhsokaRuntime::RemoveEndPoint(IAhsokaEndPoint* endPoint)
	{
		AhsokaMutexLock_t lock(RuntimeMutex);
		GetDefaultRuntime().endPoints.erase(endPoint->GetRuntimeConfiguration().service_name());
	}

	void AhsokaRuntime::ReleaseStartup()
	{
		system("systemd-notify --ready");
	}

}
