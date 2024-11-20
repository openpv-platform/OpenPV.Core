using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Dispatch;
using Ahsoka.LayerManager;
using Ahsoka.ServiceFramework;
using Ahsoka.Services;
using Ahsoka.Services.Iot;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.Services.System.Platform;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Test;

[TestClass]
public class LayoutManagerTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    //[TestMethod]
    public void TestLayoutManager()
    {
        LayoutManagerServiceClient client = new(new ServiceConfiguration()
        {
            DataChannel = 5100,
            ServiceName = LayoutManagerServiceClient.Name,
            TcpListenAddress = "*",
            TcpConnectionAddress = "openviewpro"
        });

        client.Start();


        client.UpdateSurfaceVisibility(new LayerManager.SurfaceVisibility()
        {
            SurfaceId = 1000,
            CommitChanges = true,
            IsVisible = true, 
            Opacity = .6f
               
        });

        client.UpdateSurfaceZOrder(new LayerManager.SurfaceOrder()
        {
            SurfaceIds = [121212,1000,12345],
            CommitChanges = true,
        });


        client.UpdateSurfaceLocation(new LayerManager.SurfaceRectangle()
        {
              SurfaceId = 121212,
              Width = 500,
              Height = 500,
              X = 100,
              Y = 100,
        });

        Task.Delay(2000).Wait();

        client.UpdateSurfaceSource(new LayerManager.SurfaceRectangle()
        {
            SurfaceId = 121212,
            Width = 500,
            Height = 500,
            X = 100,
            Y = 100,
        });

        Task.Delay(2000).Wait();

        client.UpdateSurfaceSetup(new LayerManager.SurfaceSetup()
        {
            SurfaceId = 121212,
            LocationRectangle = new LayerManager.SurfaceRectangle
            {
                SurfaceId = 121212,
                Width = 1024,
                Height = 600,
                X = 0,
                Y = 0
            },
            SourceRectangle = new LayerManager.SurfaceRectangle
            {
                SurfaceId = 121212,
                Width = 1024,
                Height = 600,
                X = 0,
                Y = 0
            },
            Visibility = new LayerManager.SurfaceVisibility()
            {
                IsVisible = true,
                Opacity = 1
            }
        });


        using (MemoryStream ms = new MemoryStream())
        {
            LayerManagerConfiguration configuration = new LayerManagerConfiguration();
            configuration.SurfaceSetups.Add( new LayerManager.SurfaceSetup()
            {
                SurfaceId = 121212,
                LocationRectangle = new LayerManager.SurfaceRectangle
                {
                    SurfaceId = 121212,
                    Width = 600,
                    Height = 600,
                    X = 50,
                    Y = 50
                },
                SourceRectangle = new LayerManager.SurfaceRectangle
                {
                    SurfaceId = 121212,
                    Width = 600,
                    Height = 600,
                    X = 0,
                    Y = 0
                },
                Visibility = new LayerManager.SurfaceVisibility()
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


