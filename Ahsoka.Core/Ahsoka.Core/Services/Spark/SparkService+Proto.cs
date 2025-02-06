using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Spark;

[ProtoContract]
public class SparkMessageTypes
{
    public enum Ids
    {
        None = 0,
        SetSparkSubscription = 1,
        GetSparkSubscription = 2,
        GetAvailableReleases = 4,
        GetRelease = 6,
        GetReleaseNotification = 7, //  Occurs During GetRelease
        UpdateLicenseInfo = 8,
        SubscribeToAppChange = 9,
        UnsubscribeToAppChange = 10,
        AppChangeNotification = 11,
    }
}

[ProtoContract]
public class SparkSubscription
{
    [ProtoMember(1)]
    public string ReleaseName { get; set; }

    [ProtoMember(2)]
    public string ChannelName { get; set; }

    [ProtoMember(3)]
    public string SubscriptionUrl { get; set; }

    [ProtoMember(4)]
    public string DeviceId { get; set; }

    [ProtoMember(5)]
    public bool IsFactorySubscription { get; set; }
}

[ProtoContract]
public class SparkSubscriptionInfo
{
    [ProtoMember(1)]
    public string DeviceId { get; set; }

    [ProtoMember(2)]
    public string DeviceKey { get; set; }

    [ProtoMember(3)]
    public string PartNumber { get; set; }

    [ProtoMember(4)]
    public string SerialNumber { get; set; }

    [ProtoMember(10)]
    public string ReleaseName { get; set; }

    [ProtoMember(11)]
    public string ChannelName { get; set; }

    [ProtoMember(12)]
    public string SubscriptionUrl { get; set; }

}

[ProtoContract]
public class SparkReleaseInfo
{
    [ProtoMember(1)]
    public MessageReplyStatus Result { get; set; }

    [ProtoMember(2)]
    public string ResultText { get; set; }

    [ProtoMember(3, IsRequired = true)]
    public List<SparkRelease> Releases { get; set; } = new();
}

public enum MessageReplyStatus
{
    Undetermined = 0,
    Error = 1,
    Success = 2,
    Timeout = 3,
    Continue = 4,
    Cancelled = 5,
}

[ProtoContract]
public class SparkRelease
{
    [ProtoMember(1)]
    public string ReleaseId { get; set; }

    [ProtoMember(2)]
    public SparkTrack Track { get; set; }

    [ProtoMember(3)]
    public string ChannelName { get; set; }

    [ProtoMember(4)]
    public string ReleaseName { get; set; }

    [ProtoMember(5)]
    public string ContainerName { get; set; }

    [ProtoMember(6)]
    public string ContainerVersion { get; set; }
}

[ProtoContract]
public class SparkReleaseDownloadRequest
{
    [ProtoMember(1)]
    public string ReleaseId { get; set; }

    [ProtoMember(2)]
    public string PathToFile { get; set; }

    [ProtoMember(3)]
    public int TimeoutInSeconds { get; set; }

    /// <summary>
    /// Sets the default timeout for the download to 15 seconds
    /// </summary>
    public SparkReleaseDownloadRequest()
    {
        TimeoutInSeconds = 15;
    }
}

[ProtoContract]
public class SparkReleaseDownloadNotification
{
    [ProtoMember(1)]
    public string ReleaseId { get; set; }

    [ProtoMember(2)]
    public int PercentCompleted { get; set; }

    [ProtoMember(3)]
    public MessageReplyStatus Status { get; set; }
}

[ProtoContract]
public class SparkReleaseDownloadCompleted
{
    [ProtoMember(1)]
    public string ReleaseId { get; set; }

    [ProtoMember(2)]
    public string ResultText { get; set; }

    [ProtoMember(3)]
    public MessageReplyStatus Status { get; set; }
}

[ProtoContract]
public class AppChangeInfo
{
    [ProtoMember(1)]
    public string ChannelName { get; set; }

    [ProtoMember(2)]
    public string ReleaseName { get; set; }

    [ProtoMember(3)]
    public AppChangesEnum AppEvent { get; set; }

    [ProtoMember(4)]
    public string Message { get; set; }
}

public enum SparkTrack
{
    Development = 0,
    Canary = 1,
    Beta = 2,
    Release = 3,
    NoTrack = 99,
}

public enum AppChangesEnum
{
    NoChanges = 0,
    UpdateRelease = 1,
}