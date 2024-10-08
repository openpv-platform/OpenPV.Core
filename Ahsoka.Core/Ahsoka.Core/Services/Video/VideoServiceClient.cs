using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.Video;

/// <summary>
/// Client for talking to System Service
/// </summary>
public class VideoServiceClient : AhsokaClientBase<VideoMessageTypes.Ids>
{
    /// <summary>
    /// Constructor using Default Service Configuration
    /// </summary>
    public VideoServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(VideoService.Name))
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new VideoService(this.ServiceConfig);
    }

    /// <summary>
    /// Constructor for passing a custom Service Configuration
    /// </summary>
    /// <param name="config">Service Configuration used for Connecting to the Service</param>
    public VideoServiceClient(ServiceConfiguration config) :
        base(config, new VideoServiceMessages())
    {

    }
    
    /// <summary>
    /// Gets a list of Available Video Players
    /// </summary>
    /// <returns></returns>
    public VideoPlayerInfo GetVideoPlayers()
    {
        return SendNotificationWithResponse<VideoPlayerInfo>(VideoMessageTypes.Ids.GetVideoPlayers, new EmptyNotification());
    }

    /// <summary>
    /// Begins a Playback Session on the VideoPlayer
    /// </summary>
    /// <param name="playbackRequest"></param>
    /// <returns></returns>
    public VideoStatus PlayVideo(VideoPlaybackRequest playbackRequest)
    {
        return SendNotificationWithResponse<VideoStatus>(VideoMessageTypes.Ids.PlayVideo, playbackRequest);
    }

    /// <summary>
    /// Pause Video Playback on the Video Player
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public VideoStatus PauseVideo(VideoPlayer player)
    {
        return SendNotificationWithResponse<VideoStatus>(VideoMessageTypes.Ids.PauseVideo, player);
    }

    /// <summary>
    /// Pause Video Playback on the Video Player
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public VideoStatus ResumeVideo(VideoPlayer player)
    {
        return SendNotificationWithResponse<VideoStatus>(VideoMessageTypes.Ids.ResumeVideo, player);
    }

    /// <summary>
    /// Position / Scrub Video to the Specified Position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public VideoStatus PositionVideo(VideoPosition position)
    {
        return SendNotificationWithResponse<VideoStatus>(VideoMessageTypes.Ids.PositionVideo, position);
    }


    /// <summary>
    /// Stops Video on the VideoPlayer
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public VideoStatus StopVideo(VideoPlayer player)
    {
        return SendNotificationWithResponse<VideoStatus>(VideoMessageTypes.Ids.StopVideo, player);
    }

    /// <summary>
    /// Requests Status of a VideoPlayer
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public VideoStatus RequestVideoStatus(VideoPlayer player)
    {
        return SendNotificationWithResponse<VideoStatus>(VideoMessageTypes.Ids.RequestStatus, player);
    }
}
