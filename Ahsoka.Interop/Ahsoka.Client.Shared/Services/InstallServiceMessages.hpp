#pragma once

#include <string>
#include "AhsokaMessagesBase.hpp"
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaInstall
{
    class InstallServiceMessages : public AhsokaServiceFramework::AhsokaMessagesBase
    {
        public:

            InstallServiceMessages()
            {
				RegisterServiceRequest(InstallMessageTypes_Ids_INSTALL_PACKAGE, CreateCTOR<AhsokaInstall::InstallPackageRequest>(), CreateCTOR<AhsokaInstall::InstallCompleteInfo>());
				RegisterServiceRequest(InstallMessageTypes_Ids_INSTALL_PROGRESS, CreateCTOR<AhsokaInstall::InstallProgressInfo>(), CreateCTOR<AhsokaInstall::InstallProgressInfo>());
				RegisterServiceRequest(InstallMessageTypes_Ids_INSTALL_LOG, CreateCTOR<AhsokaInstall::InstallLogInfo>(), CreateCTOR<AhsokaInstall::InstallLogInfo>());

            }
    };
}