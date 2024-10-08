#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ahsoka.Services.Video;

[AhsokaService(Name)]
public class VideoService : AhsokaServiceBase<VideoMessageTypes.Ids>
{
    #region Fields
    public const string Name = "VideoService";
    Dictionary<int, VideoPlayerImpl> players = new();
    #endregion

    #region Methods
    internal VideoPlayerImpl GetInterop(int id)
    {
        switch (SystemInfo.CurrentPlatform)
        {
            case PlatformFamily.OpenViewLinux:
            case PlatformFamily.OpenViewLinuxPro:
                return new LinuxVideoOuOfProcServiceImplementation()
                {
                    PlayerID = id,
                    VideoStatusChanged = SendStatusNotification
                };

            case PlatformFamily.Windows64:
            case PlatformFamily.Ubuntu64:
            case PlatformFamily.MacosArm64:
            default:
                return new DesktopVideoServiceImplementation() 
                {
                    PlayerID = id,
                    VideoStatusChanged = SendStatusNotification
                };


        }
    }

    public VideoService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
        
    }

    public VideoService(ServiceConfiguration config) :
        base(config, new VideoServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create VideoService");

        // Init Players
        int playerCount = SystemInfo.CurrentPlatform == PlatformFamily.OpenViewLinux ? 2 : 4; // Allow 4 on High End, 1 on Select
        // Build Video Players
        for (int id = 0; id < playerCount; id++)
            players[id] = GetInterop(id);
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<VideoMessageTypes.Ids>())
        {
            case VideoMessageTypes.Ids.GetVideoPlayers:
                HandleGetPlayers(messageHeader);
                break;
            case VideoMessageTypes.Ids.PlayVideo:
                HandlePlayVideo(messageHeader, message as VideoPlaybackRequest);
                break;
            case VideoMessageTypes.Ids.PauseVideo:
                HandlePauseVideo(messageHeader, message as VideoPlayer);
                break;
            case VideoMessageTypes.Ids.ResumeVideo:
                HandleResumeVideo(messageHeader, message as VideoPlayer);
                break;
            case VideoMessageTypes.Ids.PositionVideo:
                HandlePositionVideo(messageHeader, message as VideoPosition);
                break;
            case VideoMessageTypes.Ids.StopVideo:
                HandleStopVideo(messageHeader, message as VideoPlayer);
                break;
            case VideoMessageTypes.Ids.RequestStatus:
                HandleRequestStatus(messageHeader, message as VideoPlayer);
                break;
        }
    }

    private void HandleGetPlayers(AhsokaMessageHeader messageHeader)
    {
        var playerInfo = new VideoPlayerInfo();
        foreach (var player in players.Values)
            playerInfo.Players.Add(new VideoPlayer() { VideoPlayerID = player.PlayerID });

        SendMessageInternal(messageHeader, playerInfo);
    }

    private void HandleRequestStatus(AhsokaMessageHeader messageHeader, VideoPlayer videoPlayer)
    {
        VideoStatus status = new();

        if (players.TryGetValue(videoPlayer.VideoPlayerID,out VideoPlayerImpl value))
            status = value.GetStatus(status);

        SendMessageInternal(messageHeader, status);
    }

    private void HandlePlayVideo(AhsokaMessageHeader messageHeader, VideoPlaybackRequest videoPlaybackRequest)
    {
        VideoStatus status = new VideoStatus() { VideoState =  PlaybackState.Playing };
        if (players.TryGetValue(videoPlaybackRequest.VideoPlayerID, out VideoPlayerImpl value))
            status = value.PlayVideo(videoPlaybackRequest);

        SendMessageInternal(messageHeader, status);
    }

    private void HandlePauseVideo(AhsokaMessageHeader messageHeader, VideoPlayer player)
    {
        VideoStatus status = new VideoStatus() { VideoState = PlaybackState.Paused };
        if (players.TryGetValue(player.VideoPlayerID, out VideoPlayerImpl value))
            status = value.PauseVideo(player);

        SendMessageInternal(messageHeader, status);
    }
    private void HandleResumeVideo(AhsokaMessageHeader messageHeader, VideoPlayer player)
    {
        VideoStatus status = new VideoStatus() { VideoState = PlaybackState.Playing };
        if (players.TryGetValue(player.VideoPlayerID, out VideoPlayerImpl value))
            status = value.ResumeVideo(player);

        SendMessageInternal(messageHeader, status);
    }

    private void HandlePositionVideo(AhsokaMessageHeader messageHeader, VideoPosition position)
    {
        VideoStatus status = new VideoStatus() { VideoState = PlaybackState.Playing };
        if (players.TryGetValue(position.VideoPlayerID, out VideoPlayerImpl value))
            status = value.PositionVideo(position);

        SendMessageInternal(messageHeader, status);
    }

    private void HandleStopVideo(AhsokaMessageHeader messageHeader, VideoPlayer videoPlayer)
    {
        VideoStatus status = new VideoStatus() { VideoState = PlaybackState.Stopped };
        if (players.TryGetValue(videoPlayer.VideoPlayerID, out VideoPlayerImpl value))
            status = value.StopVideo();

        SendMessageInternal(messageHeader, status);
    }

    private void SendStatusNotification(VideoStatus e)
    {
        // Create a Header for our Notification.
        var notifHeader = new AhsokaMessageHeader() { TransportId = VideoMessageTypes.Ids.StatusUpdated.EnumToInt() };
        SendMessageInternal(notifHeader, e, true);
    }
    #endregion
}
