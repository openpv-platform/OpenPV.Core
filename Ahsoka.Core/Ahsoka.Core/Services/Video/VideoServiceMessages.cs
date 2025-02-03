using Ahsoka.Core;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.Video;

/// <summary>
///  System Service Metadata Class
/// </summary>
public class VideoServiceMessages : AhsokaMessagesBase
{
    /// <InheritDoc/>
    public VideoServiceMessages()
    {
        this.RegisterServiceRequest(VideoMessageTypes.Ids.GetVideoPlayers, typeof(EmptyNotification), typeof(VideoPlayerInfo));
        this.RegisterServiceRequest(VideoMessageTypes.Ids.PlayVideo, typeof(VideoPlaybackRequest), typeof(VideoStatus));
        this.RegisterServiceRequest(VideoMessageTypes.Ids.StopVideo, typeof(VideoPlayer), typeof(VideoStatus));
        this.RegisterServiceRequest(VideoMessageTypes.Ids.PauseVideo, typeof(VideoPlayer), typeof(VideoStatus));
        this.RegisterServiceRequest(VideoMessageTypes.Ids.ResumeVideo, typeof(VideoPlayer), typeof(VideoStatus));
        this.RegisterServiceRequest(VideoMessageTypes.Ids.PositionVideo, typeof(VideoPosition), typeof(VideoStatus));
        this.RegisterServiceRequest(VideoMessageTypes.Ids.RequestStatus, typeof(VideoPlayer), typeof(VideoStatus));
        this.RegisterServiceNotification(VideoMessageTypes.Ids.StatusUpdated, typeof(VideoStatus));
    }

    /// <InheritDoc/>
    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\VideoService.proto", Properties.Resources.VideoService);

        return result;
    }
}
