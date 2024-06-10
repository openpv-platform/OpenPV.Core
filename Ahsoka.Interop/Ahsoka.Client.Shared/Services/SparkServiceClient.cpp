#include "SparkServiceClient.h"
#include "SparkServiceMessages.hpp"
#include "AhsokaClientEndPoint.hpp"

using namespace std;
using namespace AhsokaSpark;

namespace AhsokaSpark
{
    typedef AhsokaClientEndPoint<SparkMessageTypes_Ids> SparkService_t;
	
	SparkServiceClient::SparkServiceClient():
       AhsokaServiceFramework::AhsokaEndPointWithCallback<SparkMessageTypes_Ids>(new SparkService_t(AhsokaConfiguration::GetServiceConfiguration(SparkServiceName), SparkServiceMessages()))
    {
        
    }

    SparkServiceClient::SparkServiceClient(ServiceConfiguration config):
	    AhsokaServiceFramework::AhsokaEndPointWithCallback<SparkMessageTypes_Ids>(new SparkService_t(config, SparkServiceMessages()))
    {
        
    }

	void SparkServiceClient::RegisterListener(MessageReceived_t callback)
    {
        return GetEndPoint<SparkService_t>()->NotifyMessageReceived(callback);     
    }

	void SparkServiceClient::SetSparkSubscription(SparkSubscription& sparkSubscription, int timeoutInMs)
	{
		GetEndPoint<SparkService_t>()->SendNotificationWithResponse(SparkMessageTypes_Ids_SET_SPARK_SUBSCRIPTION, &sparkSubscription, timeoutInMs);
	}

	unique_ptr<SparkSubscriptionInfo> SparkServiceClient::GetSparkSubscription(int timeoutInMs)
	{
		return GetEndPoint<SparkService_t>()->SendNotificationWithResponse<SparkSubscriptionInfo>(SparkMessageTypes_Ids_GET_SPARK_SUBSCRIPTION, 0, timeoutInMs);
	}

	unique_ptr<SparkReleaseInfo> SparkServiceClient::GetAvailableReleases(int timeoutInMs)
	{
		return GetEndPoint<SparkService_t>()->SendNotificationWithResponse<SparkReleaseInfo>(SparkMessageTypes_Ids_GET_AVAILABLE_RELEASES, 0, timeoutInMs);
	}

	unique_ptr<SparkReleaseDownloadCompleted> SparkServiceClient::GetRelease(SparkReleaseDownloadRequest& sparkReleaseDownloadRequest, int timeoutInMs)
	{
		return GetEndPoint<SparkService_t>()->SendNotificationWithResponse<SparkReleaseDownloadCompleted>(SparkMessageTypes_Ids_GET_RELEASE, &sparkReleaseDownloadRequest, timeoutInMs);
	}




	#if __has_include("SparkServiceClientExtensions.cxx") 
	# include "SparkServiceClientExtensions.cxx"
	#endif
}