using Ahsoka.Core;
using EnovationCloud.Core.DataTransferObjects;

namespace Ahsoka.Services.Spark;

internal class SparkServiceMessages : AhsokaMessagesBase
{
    public SparkServiceMessages() : base(SparkService.Name)
    {
        this.RegisterServiceRequest(SparkMessageTypes.Ids.SetSparkSubscription, typeof(SparkSubscription), typeof(EmptyNotification));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.GetSparkSubscription, typeof(EmptyNotification), typeof(SparkSubscriptionInfo));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.GetAvailableReleases, typeof(EmptyNotification), typeof(SparkReleaseInfo));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.GetRelease, typeof(SparkReleaseDownloadRequest), typeof(SparkReleaseDownloadCompleted));
        this.RegisterServiceNotification(SparkMessageTypes.Ids.GetReleaseNotification, typeof(SparkReleaseDownloadNotification));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.UpdateLicenseInfo, typeof(ModuleLicenseInfo), typeof(EmptyNotification), false, true);
        this.RegisterServiceRequest(SparkMessageTypes.Ids.SubscribeToAppChange, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.UnsubscribeToAppChange, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceNotification(SparkMessageTypes.Ids.AppChangeNotification, typeof(AppChangeInfo));
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaSpark", typeof(SparkMessageTypes.Ids));
    }
}