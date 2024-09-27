using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network;
using Ahsoka.Services.Spark;
using Ahsoka.System;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class SparkConnectionState : InstallerUIStateBase
{
    static SparkServiceClient client = null;

    const string refreshAction = "RefreshDownloads";
    const string downloadAction = "Download";
    const string installAction = "InstallPackage";

    SparkState state = SparkState.Ready;
    readonly SparkSubscriptionInfo subscriptionInfo = null;
    SparkRelease releaseInfo = null;
    static int percentComplete = 0;
    const int completeBarSizeInPixels = 350;
    string installPath;

    public SparkConnectionState()
    {
        client = CreateClient();
        subscriptionInfo = client.GetSparkSubscription();
        percentComplete = 0;
    }

    private SparkServiceClient CreateClient()
    {
        if (client != null)
            return client;

        var tempClient = new SparkServiceClient();
        tempClient.NotificationReceived += Client_NotificationReceived;
        AhsokaRuntime.Default.StartEndPoints(tempClient);
        return tempClient;
    }

    private void StopClient()
    {
        if (client != null)
        {
            client.NotificationReceived -= Client_NotificationReceived;
            AhsokaRuntime.Default.StopEndPoints(client);
            client = null;
        }
    }

    private void Client_NotificationReceived(object sender, NotificationEventArgs<SparkMessageTypes.Ids> e)
    {
        if (e.TransportId == SparkMessageTypes.Ids.GetReleaseNotification)
        {
            var notificationResult = e.NotificationObject as SparkReleaseDownloadNotification;

            if (notificationResult.Status == MessageReplyStatus.Error)
            {
                if (File.Exists(installPath))
                    File.Delete(installPath);

                installPath = null;

                // Status Error
                state = SparkState.Error;
            }
            else
            {
                percentComplete = notificationResult.PercentCompleted;
            }

            InstallerDrawingManager.Default.Refresh();
        }
        else if (e.TransportId == SparkMessageTypes.Ids.AppChangeNotification)
        {
            Console.WriteLine("App Notification Recieved");
            FetchPackages();
        }
    }

    private void WiFiViewModel_ConnectionStateChanged(object sender, EventArgs e)
    {
        // Refresh Screen
        InstallerDrawingManager.Default.Refresh();
    }

    protected override void OnDrawState(IDrawingApi drawingApi, int width, int height)
    {
        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);

        var textInfo = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 24,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        if (subscriptionInfo != null)
        {
            string subscriptionName = String.IsNullOrEmpty(subscriptionInfo.ReleaseName) ? "Default Subscription" : subscriptionInfo.ReleaseName;
            drawingApi.DrawText($"Subscribed to Spark: {subscriptionName}", width / 2, height / 2 - 30, textInfo);
        }
        else
        {
            drawingApi.DrawText($"No Spark Subscriptions", width / 2, height / 2, textInfo);
            return; // Don't Process States
        }

        textInfo.TextSize = 18;

        if (state == SparkState.Ready)
        {
            var refresh = drawingApi.CreateImage("Assets/Refresh.png");
            InstallerDrawingManager.Default.AddAction(refreshAction, InstallerDrawingManager.Default.GetActionPosition(5, refresh.Width), refresh); ;

            // Display Ready and Execute Login
            drawingApi.DrawText($"Refresh to Check for Updates", width / 2, (height / 2), textInfo);

            textInfo.TextSize = 12;
            string subUrl = String.IsNullOrEmpty(subscriptionInfo.SubscriptionUrl) ? "enovation.powervisionspark.com" : subscriptionInfo.SubscriptionUrl;

            drawingApi.DrawText($"Subscrption: {subUrl}", width / 2, height / 2 + 30, textInfo);
        }
        else if (state == SparkState.CheckingForDownloads)
        {
            // Display Ready and Execute Login
            drawingApi.DrawText($"Downloading Package List", width / 2, height / 2, textInfo);
        }
        else if (state == SparkState.DownloadingPackage)
        {
            DrawDownloading(drawingApi, width, height);
        }
        else if (state == SparkState.DisplayingDownloads)
        {
            // Display Ready and Execute Login
            if (releaseInfo != null)
            {
                drawingApi.DrawText($"Update Available", width / 2, height / 2, textInfo);
                drawingApi.DrawText($"{releaseInfo.ReleaseName} {releaseInfo.ContainerVersion}", width / 2, height / 2 + 30, textInfo);

                var right = drawingApi.CreateImage("Assets/ArrowRight.png");
                InstallerDrawingManager.Default.AddAction(downloadAction, InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen);
            }
            else
            {
                drawingApi.DrawText($"No Updates Available", width / 2, height / 2, textInfo);
            }

            var refresh = drawingApi.CreateImage("Assets/Refresh.png");
            InstallerDrawingManager.Default.AddAction(refreshAction, InstallerDrawingManager.Default.GetActionPosition(5, refresh.Width), refresh); ;

        }
        else if (state == SparkState.PackageReady)
        {
            drawingApi.DrawText($"Update Ready To Install", width / 2, height / 2, textInfo);
            var right = drawingApi.CreateImage("Assets/ArrowRight.png");
            InstallerDrawingManager.Default.AddAction(installAction, InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen); ;
        }
        else if (state == SparkState.Error)
        {
            // Display Ready and Execute Login
            drawingApi.DrawText($"Error Downloading", width / 2, height / 2, textInfo);

            var refresh = drawingApi.CreateImage("Assets/Refresh.png");
            InstallerDrawingManager.Default.AddAction(refreshAction, InstallerDrawingManager.Default.GetActionPosition(5, refresh.Width), refresh); ;
        }
        else if (state == SparkState.NoPackages)
        {
            // Display Ready and Execute Login
            drawingApi.DrawText($"No Packages Found to Download", width / 2, height / 2, textInfo);

            var refresh = drawingApi.CreateImage("Assets/Refresh.png");
            InstallerDrawingManager.Default.AddAction(refreshAction, InstallerDrawingManager.Default.GetActionPosition(5, refresh.Width), refresh); ;
        }
    }

    private void DrawDownloading(IDrawingApi drawingApi, int width, int height)
    {
        float rectHeight = 25f;
        float rectWidth = percentComplete / 100f * completeBarSizeInPixels;
        float left = +(width / 2) - (completeBarSizeInPixels) / 2;
        float top = height / 2 - rectHeight / 2;

        drawingApi.DrawRoundRectangle(new DrawingRect(left, top, completeBarSizeInPixels, rectHeight), 5f, DrawingColors.DarkGray, DrawingColors.Gainsboro, 1);

        drawingApi.SetClip(new DrawingRect(left, top, rectWidth, rectHeight));
        drawingApi.DrawRoundRectangle(new DrawingRect(left, top, completeBarSizeInPixels, rectHeight), 5f, DrawingColors.Transparent, DrawingColors.DodgerBlue, 0);
        drawingApi.ClearClip();


        var textInfo = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 16,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"Downloading {releaseInfo.ContainerName} from Spark...", width / 2, 40 + height / 2, textInfo);
    }

    private void FetchPackages()
    {
        // Change State While we execute the Download
        state = SparkState.CheckingForDownloads;
        InstallerDrawingManager.Default.Refresh();

        Task.Run(() =>
        {
            var result = client.GetAvailableReleases();
            client.SubscribeToAppChanges();
            Console.WriteLine($"Received {result.Releases.Count}");
            return result;

        }).ContinueWith((releases) =>
        {
            Console.WriteLine("Received Releases - Displaying Downloads State");

            if (releases.IsCompletedSuccessfully && releases.Result?.Result == MessageReplyStatus.Success)
            {
                releaseInfo = releases.Result.Releases.FirstOrDefault();

                Console.WriteLine("Successful Download");
                state = SparkState.DisplayingDownloads;
                InstallerDrawingManager.Default.Refresh();
            }
            else if (releases.IsCompletedSuccessfully)
            {
                Console.WriteLine("No Releases Available");
                state = SparkState.NoPackages;
                InstallerDrawingManager.Default.Refresh();
            }
            else
            {
                Console.WriteLine("Error During Release Check");
                state = SparkState.Error;
                InstallerDrawingManager.Default.Refresh();
            }
        });
    }

    private void DownloadPackage()
    {
        // Change State While we execute the Download
        state = SparkState.DownloadingPackage;
        InstallerDrawingManager.Default.Refresh();

        installPath = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName();
        Console.WriteLine($"Requesting Download to {installPath}");

        // Download in Background.
        var task = Task.Run(() =>
        {
            var result = client.DownloadApplication(new SparkReleaseDownloadRequest()
            {
                ReleaseId = releaseInfo.ReleaseId,
                PathToFile = installPath,
                TimeoutInSeconds = 20
            });

            if (result.Status == MessageReplyStatus.Success)
            {
                // Status Completed
                state = SparkState.PackageReady;

                InstallerDrawingManager.Default.Refresh();

            }
        });
    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        if (InstallerDrawingManager.Default.WiFiViewModel.ConnectionState != WirelessConnectionState.ClientConnected)
        {
            return new WiFiSetupState();
        }
        else if (action == refreshAction)
        {
            FetchPackages();
        }
        else if (action == "Cancel")
        {
            if (state == SparkState.PackageReady)
            {
                state = SparkState.Ready;
            }
            else
            {
                StopClient();
                return new WiFiSetupState();
            }
        }
        else if (action == downloadAction)
        {
            DownloadPackage();
        }
        else if (action == installAction)
        {
            return new InstallingState(installPath);
        }

        return this;
    }


}

public enum SparkState
{
    Ready,
    CheckingForDownloads,
    DisplayingDownloads,
    DownloadingPackage,
    PackageReady,
    Error,
    NoPackages
}
