using Ahsoka.ServiceFramework;
using EnovationCloud.Core.DataTransferObjects;

namespace Ahsoka.Services.Spark;

internal class SparkServiceMessages : AhsokaMessagesBase
{
    public SparkServiceMessages()
    {
        this.RegisterServiceRequest(SparkMessageTypes.Ids.SetSparkSubscription, typeof(SparkSubscription), typeof(EmptyNotification));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.GetSparkSubscription, typeof(EmptyNotification), typeof(SparkSubscriptionInfo));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.GetAvailableReleases, typeof(EmptyNotification), typeof(SparkReleaseInfo));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.GetRelease, typeof(SparkReleaseDownloadRequest), typeof(SparkReleaseDownloadCompleted));
        this.RegisterServiceNotification(SparkMessageTypes.Ids.GetReleaseNotification, typeof(SparkReleaseDownloadNotification));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.UpdateLicenseInfo, typeof(ModuleLicenseInfo), typeof(EmptyNotification), false, true);
    }
}

