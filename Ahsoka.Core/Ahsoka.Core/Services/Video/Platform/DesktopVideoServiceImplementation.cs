using System;

namespace Ahsoka.Services.Video;

/// <summary>
/// Basic Simulation of Video Playback Service
/// </summary>
internal class DesktopVideoServiceImplementation : VideoPlayerImpl
{
    PlaybackState playbackState = PlaybackState.Ready;
    DateTime endTime;
    DateTime startTime;
    public DesktopVideoServiceImplementation()
    {

    }

    protected override VideoStatus OnStartVideo(VideoPlaybackRequest videoPlaybackRequest)
    {
        startTime = DateTime.Now;
        endTime = DateTime.Now.AddMinutes(1); // Simulated 1 Minute
        playbackState = PlaybackState.Playing;

        return CreateStatus();
    }

    protected override VideoStatus OnPauseVideo()
    {
        startTime = DateTime.Now;
        endTime = DateTime.Now.AddMinutes(1); // Simulated 1 Minute
        playbackState = PlaybackState.Paused;

        return CreateStatus();
    }

    protected override VideoStatus OnResumeVideo()
    {
        startTime = DateTime.Now;
        endTime = DateTime.Now.AddMinutes(1); // Simulated 1 Minute
        playbackState = PlaybackState.Playing;

        return CreateStatus();
    }

    protected override VideoStatus OnPositionVideo(VideoPosition position)
    {
        startTime = DateTime.Now;
        endTime = DateTime.Now.AddMinutes(1); // Simulated 1 Minute
        playbackState = PlaybackState.Playing;

        return CreateStatus();
    }

    protected override VideoStatus OnStopVideo()
    {
        playbackState = PlaybackState.Stopped;
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
            VideoState = playbackState,
            TotalTimeInMs = (int)(endTime - startTime).TotalMilliseconds,
            PositionInMs = (int)(DateTime.Now - startTime).TotalMilliseconds,
            VideoPlayerID = this.PlayerID
        };
    }
}