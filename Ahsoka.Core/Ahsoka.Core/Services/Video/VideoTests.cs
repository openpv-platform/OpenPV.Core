using Ahsoka.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ahsoka.Services.Video;
[ExcludeFromCodeCoverage]
internal class VideoTests
{
    static AutoResetEvent resetEvent = new AutoResetEvent(false);
    internal static void TestVideo(string videoPath)
    {
        // Start SystemService
        Console.WriteLine($"TEST: Starting Runtimes for Video Test");

        // Start client for NetworkService
        var client = new VideoServiceClient();


        client.NotificationReceived += (o, e) =>
        {
            if (e.TransportId == VideoMessageTypes.Ids.StatusUpdated)
            {
                VideoStatus status = (VideoStatus)e.NotificationObject;
                Console.WriteLine($"TEST: Player {status.VideoPlayerID}  State:{status.VideoState} ({status.PositionInMs / 1000.0f}s/{status.TotalTimeInMs / 1000.0f}s)");

                // Video 1 Finished
                if (status.VideoState == PlaybackState.Stopped && status.VideoPlayerID == 0)
                    resetEvent.Set();
            }
        };

        client.Start();
        Console.WriteLine($"TEST: Service Started");

        var count = client.GetVideoPlayers().Players.Count;

        client.PlayVideo(new VideoPlaybackRequest()
        {
            PlayFullScreen = false,
            StatusUpdateInMs = 1000,
            VideoURL = videoPath,
            VideoPlayerID = 0,
            VideoHeight = 300,
            VideoWidth = 600,
        });

        Thread.Sleep(10000);

        if (count > 1)
        {
            client.PlayVideo(new VideoPlaybackRequest()
            {
                PlayFullScreen = false,
                StatusUpdateInMs = 1000,
                VideoURL = videoPath,
                VideoPlayerID = 1,
                VideoHeight = 200,
                VideoWidth = 300,
            });

            Thread.Sleep(5000);

            Console.WriteLine($"TEST: Stoping Video on Player 0");
            client.StopVideo(new VideoPlayer() { VideoPlayerID = 1 });
        }


        Console.WriteLine($"TEST: Pausing Video on Player 0");
        client.PauseVideo(new VideoPlayer() { VideoPlayerID = 0 });
        Thread.Sleep(5000);
        Console.WriteLine($"TEST: Resuming Video");
        client.ResumeVideo(new VideoPlayer() { VideoPlayerID = 0 });

        Thread.Sleep(5000);
        Console.WriteLine($"TEST: Fast Forwarding Video to 170s ");
        client.PositionVideo(new() { VideoPlayerID = 0, PositionInMs = 170000 });

        Console.WriteLine($"TEST: Waiting for End of Video");
        resetEvent.WaitOne();

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }
}