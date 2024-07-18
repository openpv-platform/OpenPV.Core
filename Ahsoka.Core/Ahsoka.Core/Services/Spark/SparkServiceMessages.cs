using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using EnovationCloud.Core.DataTransferObjects;
using System.Collections.Generic;

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
        this.RegisterServiceRequest(SparkMessageTypes.Ids.SubscribeToAppChange, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(SparkMessageTypes.Ids.UnsubscribeToAppChange, typeof(EmptyNotification), typeof(EmptyNotification));
        this.RegisterServiceNotification(SparkMessageTypes.Ids.AppChangeNotification, typeof(AppChangeInfo));
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\SparkService.proto", Properties.Resources.SparkService);
        return result;
    }
}

