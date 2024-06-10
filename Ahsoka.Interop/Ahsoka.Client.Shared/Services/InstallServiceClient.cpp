#include "InstallServiceClient.h"
#include "InstallServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaInstall;

namespace AhsokaInstall
{
    typedef AhsokaClientEndPoint<InstallMessageTypes_Ids> InstallService_t;
	
	InstallServiceClient::InstallServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<InstallMessageTypes_Ids>(new InstallService_t(AhsokaConfiguration::GetServiceConfiguration(InstallServiceName), InstallServiceMessages()))
    {
        
    }

    InstallServiceClient::InstallServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<InstallMessageTypes_Ids>(new InstallService_t(config, InstallServiceMessages()))
    {
        
    }

	void InstallServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<InstallService_t>()->NotifyMessageReceived(callback);     
    }

	unique_ptr<InstallCompleteInfo> InstallServiceClient::InstallPackage(InstallPackageRequest& installPackageRequest, int timeoutInMs)
	{
		return GetEndPoint<InstallService_t>()->SendNotificationWithResponse<InstallCompleteInfo>(InstallMessageTypes_Ids_INSTALL_PACKAGE, &installPackageRequest, timeoutInMs);
	}

	unique_ptr<InstallProgressInfo> InstallServiceClient::InstallProgress(InstallProgressInfo& installProgressInfo, int timeoutInMs)
	{
		return GetEndPoint<InstallService_t>()->SendNotificationWithResponse<InstallProgressInfo>(InstallMessageTypes_Ids_INSTALL_PROGRESS, &installProgressInfo, timeoutInMs);
	}

	unique_ptr<InstallLogInfo> InstallServiceClient::InstallLog(InstallLogInfo& installLogInfo, int timeoutInMs)
	{
		return GetEndPoint<InstallService_t>()->SendNotificationWithResponse<InstallLogInfo>(InstallMessageTypes_Ids_INSTALL_LOG, &installLogInfo, timeoutInMs);
	}




	#if __has_include("InstallServiceClientExtensions.cxx") 
	# include "InstallServiceClientExtensions.cxx"
	#endif
}