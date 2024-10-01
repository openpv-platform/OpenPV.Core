using Ahsoka.ServiceFramework;
using Ahsoka.Services.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class VideoServiceTest : LinearTestBase
{
    public TestContext TestContext { get; set; }
    AutoResetEvent waitForStatus = new AutoResetEvent(false);

    [TestMethod]
    public void TestVideoService()
    {
        VideoServiceClient service = new VideoServiceClient();
        service.NotificationReceived += (o, e) =>
        {
            if (e.TransportId == VideoMessageTypes.Ids.StatusUpdated)
                waitForStatus.Set();
        };

        foreach (var item in service.GetVideoPlayers().Players)
        {
            var status =  service.PlayVideo(new VideoPlaybackRequest() 
            {
                VideoPlayerID = item.VideoPlayerID, 
                StatusUpdateInMs = 250 
            });
            
            Assert.IsTrue(status.IsPlaying);
            status = service.RequestVideoStatus(item);
            Assert.IsTrue(status.IsPlaying);

            var result = waitForStatus.WaitOne(5000);
            Assert.IsTrue(result, "Error - Status Did Not Return");

            service.StopVideo(item);
            status = service.RequestVideoStatus(item);
            Assert.IsTrue(!status.IsPlaying);
        }


        AhsokaRuntime.ShutdownAll();
    }


}

