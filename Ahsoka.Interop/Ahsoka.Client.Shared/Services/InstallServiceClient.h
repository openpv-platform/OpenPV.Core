#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaInstall;

#if __has_include("InstallServiceClientIncludes.h") 
# include "InstallServiceClientIncludes.h"
#endif

namespace AhsokaInstall
{
	const std::string InstallServiceName = "InstallService";

	class InstallServiceClient : public AhsokaServiceFramework::AhsokaEndPointWithCallback<InstallMessageTypes_Ids>
	{
		public:

			InstallServiceClient();
			InstallServiceClient(ServiceConfiguration config);

			void RegisterListener(MessageReceived_t callback);

			// The Following Notifications Are Available at the RegisterListenerCallback

			unique_ptr<InstallCompleteInfo> InstallPackage(InstallPackageRequest& installPackageRequest, int timeoutInMs = INT_MAX);
			unique_ptr<InstallProgressInfo> InstallProgress(InstallProgressInfo& installProgressInfo, int timeoutInMs = INT_MAX);
			unique_ptr<InstallLogInfo> InstallLog(InstallLogInfo& installLogInfo, int timeoutInMs = INT_MAX);


			#if __has_include("InstallServiceClientExtensions.h") 
			# include "InstallServiceClientExtensions.h"
			#endif
	};
}