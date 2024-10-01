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
            if (!currentVideoStatus.IsPlaying && !timerTask.IsCompleted)
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

    private void StartPlaybackTimer(VideoPlaybackRequest videoPlaybackRequest)
    {
        if (currentVideoStatus.IsPlaying && videoPlaybackRequest.StatusUpdateInMs > 0)
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

    protected virtual VideoStatus OnStartVideo(VideoPlaybackRequest videoPlaybackRequest) { return new VideoStatus() { IsPlaying = true }; }
    protected virtual VideoStatus OnStopVideo() { return new VideoStatus() { IsPlaying = false }; }
    protected virtual VideoStatus OnGetVideoStatus() { return new VideoStatus() { IsPlaying = false }; }

    public class VideoTimer
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                    action();
            }

        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}