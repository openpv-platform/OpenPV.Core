using Ahsoka.ServiceFramework;
using Ahsoka.System;
using EnovationCloud.Core.DataTransferObjects;

namespace Ahsoka.Services.Spark;

/// <summary>
/// Client used to talk to Spark IOT and OTA Services
/// </summary>
public class SparkServiceClient : AhsokaClientBase<SparkMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor which uses Default Service Configuration
    /// </summary>
    public SparkServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(SparkService.Name))
    {

    }

    /// <summary>
    /// Constructor used to specify a custom Service Configuration
    /// </summary>
    /// <param name="config"></param>
    public SparkServiceClient(ServiceConfiguration config) :
        base(config, new SparkServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new SparkService(this.ServiceConfig);
    }


    /// <summary>
    /// Gets information about the current Spark Subscription including the URL, DeviceID Used and other Subscription Values.
    /// </summary>
    /// <param name="timeoutInMs"></param>
    /// <returns>Subscription Info containing details about the subscription.</returns>
    public SparkSubscriptionInfo GetSparkSubscription(int timeoutInMs = int.MaxValue)
    {
        return SendNotificationWithResponse<SparkSubscriptionInfo>(SparkMessageTypes.Ids.GetSparkSubscription, new EmptyNotification(), timeoutInMs);
    }

    /// <summary>
    /// Updates the Subscription Info on the Display.  Note that this is distructive. This will subscribe the 
    /// target to the new subscription information and it will no longer be able to access old subscription data.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="timeoutInMs"></param>
    public void SetSparkSubscription(SparkSubscription subscription, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(SparkMessageTypes.Ids.SetSparkSubscription, subscription);
    }

    /// <summary>
    /// Gets a list of releases that are available in the Spark OTA List for this Device.
    /// </summary>
    /// <returns></returns>
    public SparkReleaseInfo GetAvailableReleases()
    {
        return SendNotificationWithResponse<SparkReleaseInfo>(SparkMessageTypes.Ids.GetAvailableReleases);
    }

    /// <summary>
    /// Executes a Download of the Specified Application for use in Updating the Customer Application
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public SparkReleaseDownloadCompleted DownloadApplication(SparkReleaseDownloadRequest request)
    {
        return SendNotificationWithResponse<SparkReleaseDownloadCompleted>(SparkMessageTypes.Ids.GetRelease, request);
    }

    internal void UpdateLicenseInfo(ModuleLicenseInfo licenseInfo)
    {
        SendNotificationWithResponse<EmptyNotification>(SparkMessageTypes.Ids.UpdateLicenseInfo, licenseInfo);
    }
}

/// <summary>
/// Details for a request to download a Spark Release
/// </summary>
public partial class SparkReleaseDownloadRequest
{
    /// <summary>
    /// Sets the default timeout for the download to 15 seconds
    /// </summary>
    public SparkReleaseDownloadRequest()
    {
        TimeoutInSeconds = 15;
    }
}

