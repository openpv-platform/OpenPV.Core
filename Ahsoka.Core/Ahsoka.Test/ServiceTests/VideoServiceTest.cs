using Ahsoka.Core;
using Ahsoka.Services.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class VideoServiceTest : LinearTestBase
{
    public TestContext TestContext { get; set; }
    AutoResetEvent waitForPlay = new AutoResetEvent(false);
    AutoResetEvent waitForPause = new AutoResetEvent(false);

    [TestMethod]
    public void TestVideoService()
    {
        VideoServiceClient service = new VideoServiceClient();
        service.NotificationReceived += (o, e) =>
        {
            if (e.TransportId == VideoMessageTypes.Ids.StatusUpdated)
            {
                if (e.NotificationObject is VideoStatus status)
                {
                    if (status.VideoState == PlaybackState.Playing)
                        waitForPlay.Set();
                    if (status.VideoState == PlaybackState.Paused)
                        waitForPause.Set();
                }
            }

        };

        foreach (var item in service.GetVideoPlayers().Players)
        {
            var status = service.PlayVideo(new VideoPlaybackRequest()
            {
                VideoPlayerID = item.VideoPlayerID,
                StatusUpdateInMs = 250
            });

            Assert.IsTrue(status.VideoState == PlaybackState.Playing);
            status = service.RequestVideoStatus(item);
            Assert.IsTrue(status.VideoState == PlaybackState.Playing);

            // Wait for Notification of Play
            var result = waitForPlay.WaitOne(5000);
            Assert.IsTrue(result, "Error - Status Did Not Return");

            service.PositionVideo(new VideoPosition() { PositionInMs = 1000 });

            // Wait for Notification for Pause
            service.PauseVideo(item);
            result = waitForPause.WaitOne(5000);
            Assert.IsTrue(result, "Error - Status Did Not Return");

            service.StopVideo(item);
            status = service.RequestVideoStatus(item);
            Assert.IsTrue(status.VideoState == PlaybackState.Stopped);
        }


        AhsokaRuntime.Default.StopAllEndPoints();
    }


}

