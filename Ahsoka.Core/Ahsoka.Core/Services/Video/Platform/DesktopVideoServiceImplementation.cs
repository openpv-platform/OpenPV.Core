using System;

namespace Ahsoka.Services.Video;

/// <summary>
/// Basic Simulation of Video Playback Service
/// </summary>
internal class DesktopVideoServiceImplementation : VideoPlayerImpl
{
    bool isPlayingStatus = false;
    DateTime endTime;
    DateTime startTime;
    public DesktopVideoServiceImplementation()
    {

    }

    protected override VideoStatus OnStartVideo(VideoPlaybackRequest videoPlaybackRequest)
    {
        startTime = DateTime.Now;
        endTime = DateTime.Now.AddMinutes(1); // Simulated 1 Minute
        isPlayingStatus = true;

        return CreateStatus();
    }

    protected override VideoStatus OnStopVideo()
    {
        isPlayingStatus = false;
        endTime = startTime = DateTime.MaxValue;
        return CreateStatus();
    }

    protected override VideoStatus OnGetVideoStatus()
    {
        return CreateStatus();
    }

    private VideoStatus CreateStatus()
    {
        return new VideoStatus()
        {
            IsPlaying = isPlayingStatus,
            TotalTimeInMs = (int)(endTime - startTime).TotalMilliseconds,
            PositionInMs = (int)(DateTime.Now - startTime).TotalMilliseconds,
            VideoPlayerID = this.PlayerID
        };
    }
}