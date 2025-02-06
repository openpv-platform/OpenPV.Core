using Ahsoka.Core;

namespace Ahsoka.Services.Video;

/// <summary>
///  System Service Metadata Class
/// </summary>
public class VideoServiceMessages : AhsokaMessagesBase
{
    /// <InheritDoc/>
    public VideoServiceMessages() : base(VideoService.Name)
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
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaVideo", typeof(VideoMessageTypes.Ids));
    }
}
