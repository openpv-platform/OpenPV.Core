#pragma warning disable CS1591
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Ahsoka.Services.Video;

internal class VideoPlayerImpl
{
    CancellationTokenSource cancellationTokenSource;
    Task timerTask = null;
    private object syncRoot = new object();
    private VideoStatus currentVideoStatus;

    public int PlayerID { get; init; }
    public Action<VideoStatus> VideoStatusChanged { get; init; }

    internal VideoStatus GetStatus(VideoStatus status)
    {
        lock (syncRoot)
            return currentVideoStatus;
    }

    internal VideoStatus StopVideo()
    {
        lock (syncRoot)
        {
            currentVideoStatus = OnStopVideo();

            // Wait for Timer Cancel
            if (timerTask != null && !timerTask.IsCompleted)
                cancellationTokenSource.Cancel();

            return currentVideoStatus;
        }
    }

    internal VideoStatus PlayVideo(VideoPlaybackRequest videoPlaybackRequest)
    {
        lock (syncRoot)
        {
            currentVideoStatus = OnStartVideo(videoPlaybackRequest);

            StartPlaybackTimer(videoPlaybackRequest);

            return currentVideoStatus;
        }
    }

    internal VideoStatus ResumeVideo(VideoPlayer player)
    {
        lock (syncRoot)
            return OnResumeVideo();
    }

    internal VideoStatus PauseVideo(VideoPlayer player)
    {
        lock (syncRoot)
            return OnPauseVideo();
    }

    internal VideoStatus PositionVideo(VideoPosition position)
    {
        lock (syncRoot)
            return OnPositionVideo(position);
    }


    protected virtual void StartPlaybackTimer(VideoPlaybackRequest videoPlaybackRequest)
    {
        if (currentVideoStatus.VideoState == PlaybackState.Playing && videoPlaybackRequest.StatusUpdateInMs > 0)
        {
            cancellationTokenSource = new CancellationTokenSource();
            timerTask = VideoTimer.Run(() =>
            {
                lock (syncRoot)
                {
                    currentVideoStatus = OnGetVideoStatus();
                    VideoStatusChanged?.Invoke(currentVideoStatus);
                }
            },
            TimeSpan.FromMilliseconds(videoPlaybackRequest.StatusUpdateInMs), cancellationTokenSource.Token);
        }
    }

    protected virtual VideoStatus OnStartVideo(VideoPlaybackRequest videoPlaybackRequest) { return new VideoStatus() { VideoState = PlaybackState.Playing }; }
    protected virtual VideoStatus OnStopVideo() { return new VideoStatus() {  VideoState =  PlaybackState.Stopped }; }
    protected virtual VideoStatus OnGetVideoStatus() { return new VideoStatus() { VideoState = PlaybackState.Playing }; }
    protected virtual VideoStatus OnPauseVideo() { return new VideoStatus() { VideoState = PlaybackState.Paused }; }
    protected virtual VideoStatus OnResumeVideo() { return new VideoStatus() { VideoState = PlaybackState.Paused }; }
    protected virtual VideoStatus OnPositionVideo(VideoPosition position) { return new VideoStatus() { VideoState = PlaybackState.Playing }; }

    public class VideoTimer
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(period, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                        action();
                }
            }
            catch (TaskCanceledException) { }

        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}