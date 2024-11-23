using Ahsoka.Services.LayerManager;
using Ahsoka.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Test;

//[TestClass]
public class LayoutManagerTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestLayoutManager()
    {
        LayerManagerServiceClient client = new(new ServiceConfiguration()
        {
            DataChannel = 5100,
            ServiceName = LayerManagerServiceClient.Name,
            TcpListenAddress = "*",
            TcpConnectionAddress = "openviewpro"
        });

        client.Start();

        var info = client.GetSurfaceSetup(new SurfaceRequest() { SurfaceId = 121212 });

        client.UpdateSurfaceVisibility(new SurfaceVisibility()
        {
            SurfaceId = 1000,
            CommitChanges = true,
            IsVisible = true, 
            Opacity = .6f
               
        });

        client.UpdateSurfaceZOrder(new SurfaceOrder()
        {
            SurfaceIds = [121212,1000,12345],
            CommitChanges = true,
        });


        client.UpdateSurfaceLocation(new SurfaceRectangle()
        {
              SurfaceId = 121212,
              Width = 500,
              Height = 500,
              X = 100,
              Y = 100,
        });

        Task.Delay(2000).Wait();

        client.UpdateSurfaceSource(new SurfaceRectangle()
        {
            SurfaceId = 121212,
            Width = 500,
            Height = 500,
            X = 100,
            Y = 100,
        });

        Task.Delay(2000).Wait();

        client.UpdateSurfaceSetup(new SurfaceSetup()
        {
            SurfaceId = 121212,
            LocationRectangle = new SurfaceRectangle
            {
                SurfaceId = 121212,
                Width = 1024,
                Height = 600,
                X = 0,
                Y = 0
            },
            SourceRectangle = new SurfaceRectangle
            {
                SurfaceId = 121212,
                Width = 1024,
                Height = 600,
                X = 0,
                Y = 0
            },
            Visibility = new SurfaceVisibility()
            {
                IsVisible = true,
                Opacity = 1
            }
        });


        using (MemoryStream ms = new MemoryStream())
        {
            LayerManagerConfiguration configuration = new LayerManagerConfiguration();
            configuration.SurfaceSetups.Add( new SurfaceSetup()
            {
                SurfaceId = 121212,
                LocationRectangle = new SurfaceRectangle
                {
                    SurfaceId = 121212,
                    Width = 600,
                    Height = 600,
                    X = 50,
                    Y = 50
                },
                SourceRectangle = new SurfaceRectangle
                {
                    SurfaceId = 121212,
                    Width = 600,
                    Height = 600,
                    X = 0,
                    Y = 0
                },
                Visibility = new SurfaceVisibility()
                {
                    IsVisible = true,
                    Opacity = 1.0f
                }
            });
            ProtoBuf.Serializer.Serialize(ms, configuration);
            File.WriteAllBytes(@"C:\Users\Jake\Desktop\SurfaceConfiguration.pbuff", ms.ToArray());

        }
    }

}


