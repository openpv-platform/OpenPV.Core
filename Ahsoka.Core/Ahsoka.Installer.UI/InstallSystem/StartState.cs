using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Services.Install;
using Ahsoka.System;
using QRCoder;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class StartState : InstallerUIStateBase
{
    readonly string url = $"https://support.enovationcontrols.com/hc/en-us";
    readonly string pathToImage = Path.Combine(Path.GetTempPath(), "3A5756C7-90C3-43F7-B25F-2C68100BF898.png");

    bool showHelp = false;
    const int imageSize = 196;
    const string shutdown = "SHUTDOWN";
    const string usb = "USB";
    const string reset = "RESET";
    const string cancel_reset = "CANCEL_RESET";
    const string app = "APP";
    const string confirmApp = "CONFIRM_APP";
    const string factory = "FACTORY";
    const string confirmfactory = "CONFIRM_FACTORY";
    const string cloud = "CLOUD";
    const string ethernet = "ETHERNET";
    static bool isReady = false;
    bool showResetOptions = false;
    bool showFactoryConfirm = false;
    bool showAppConfirm = false;
    bool isDeleting = false;

    static bool hasNetwork = false;
    readonly Timer timer = new(15000);
    DrawingImage qrBitmap = null;

    public void Init(bool hasNetwork)
    {
        StartState.isReady = true;
        StartState.hasNetwork = hasNetwork;
        timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        timer.Stop();
        showHelp = !showHelp;
        InstallerDrawingManager.Default.HandleAction("?", out bool _);
        InstallerDrawingManager.Default.Invalidate();
    }

    public StartState()
    {
        Task.Run(() =>
        {
            Thread.Sleep(5000);
            if (!File.Exists(pathToImage))
            {
                QRCodeGenerator qrGenerator = new();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);
                File.WriteAllBytes(pathToImage, qrCodeImage);
            }
        });
    }

    protected override void OnDrawState(DrawFrameArgs args)
    {
        IDrawingApi drawingApi = args.DrawingApi;
        int width = args.Width;
        int height = args.Height;

        if (isReady && !showHelp && !showResetOptions && !isDeleting)
        {
            timer.Start();

            hasNetwork = false;

            DrawingRect usbPosition = new(width * (hasNetwork ? .25f : .38f) - imageSize / 2 - 20,
                height / 2 - imageSize / 2,
                imageSize,
                imageSize);

            DrawingRect cloudPosition = new(width * (hasNetwork ? .75f : .64f) - imageSize / 2 + 20,
                height / 2 - imageSize / 2,
                imageSize,
                imageSize);

            var shutdownImage = drawingApi.CreateImage("Assets/exit_icon.png");
            InstallerDrawingManager.Default.AddAction(shutdown, InstallerDrawingManager.Default.GetActionPosition(1, shutdownImage.Width), shutdownImage);

            var helpBitmap = drawingApi.CreateImage("Assets/help_icon.png");
            InstallerDrawingManager.Default.AddAction("?", InstallerDrawingManager.Default.GetActionPosition(4, shutdownImage.Width), helpBitmap);

            var usbImage = drawingApi.CreateImage("Assets/USB.png");
            InstallerDrawingManager.Default.AddAction(usb, usbPosition, usbImage, DrawingColors.DarkSeaGreen);

            var cloudImage = drawingApi.CreateImage("Assets/Cloud.png");
            InstallerDrawingManager.Default.AddAction(cloud, cloudPosition, cloudImage, DrawingColors.LightSkyBlue);

            var resetImage = drawingApi.CreateImage("Assets/Reset.png");
            InstallerDrawingManager.Default.AddAction(reset, InstallerDrawingManager.Default.GetActionPosition(5, resetImage.Width), resetImage, DrawingColors.Gainsboro);

            var textPaint = new DrawingTextInfo
            {
                Color = new DrawingColor(0xFF111111),
                TextSize = 40,
                Alignment = DrawingTextAlignment.Center,
                Typeface = InstallerDrawingManager.Default.UITypeface,
                TextWeight = DrawingTextWeight.Bold
            };

            drawingApi.DrawText($"Select an Install Method", width / 2, height / 2 - imageSize / 2 - 40, textPaint);
        }
        else if (showHelp)
        {
            DrawingRect usbPosition = new(width * (.33f) - imageSize / 2 - 20,
                 height / 2 - imageSize / 2,
                 imageSize,
                 imageSize);

            if (qrBitmap == null && File.Exists(pathToImage))
                qrBitmap = drawingApi.CreateImage(pathToImage);

            if (qrBitmap != null)
                drawingApi.DrawBitmap(qrBitmap, usbPosition);

            var textPaint = new DrawingTextInfo
            {
                Color = new DrawingColor(0xFF111111),
                TextSize = 40,
                Alignment = DrawingTextAlignment.Center,
                Typeface = InstallerDrawingManager.Default.UITypeface
            };

            drawingApi.DrawText($"Need Help with OpenView and OpenPV", width / 2, height / 4 - 40, textPaint);

            textPaint.TextSize = 30;
            textPaint.Color = DrawingColors.SteelBlue;
            textPaint.Alignment = DrawingTextAlignment.Left;

            drawingApi.DrawText($"Scan Here for Info", usbPosition.X + usbPosition.Width + 20, usbPosition.Y + 100, textPaint);
            textPaint.TextSize = 15;
            textPaint.TextWeight = DrawingTextWeight.Bold;
            drawingApi.DrawText(url, usbPosition.X + usbPosition.Width + 20, usbPosition.Y + 120, textPaint);

            DrawingRect rect = new(0, 0, drawingApi.ScreenWidth, drawingApi.ScreenHeight);
            InstallerDrawingManager.Default.AddAction("Support", rect, "");
        }
        else if (isDeleting)
        {
            var textPaint = new DrawingTextInfo
            {
                Color = new DrawingColor(0xFF111111),
                TextSize = 40,
                Alignment = DrawingTextAlignment.Center,
                Typeface = InstallerDrawingManager.Default.UITypeface,
                TextWeight = DrawingTextWeight.Bold
            };

            if (showAppConfirm)
            {
                drawingApi.DrawText($"Deleting Application Data", width / 2, height / 2, textPaint);
                ApplicationDataPaths.ClearAppData(true, false);
            }

            if (showFactoryConfirm)
            {
                drawingApi.DrawText($"Deleting All Data", width / 2, height / 2, textPaint);
                ApplicationDataPaths.ClearAppData(true, true);
            }

            InstallerDrawingManager.Default.Invalidate();


        }
        else if (showResetOptions)
        {
            int dialogHeight = 300;
            int dialogWidth = 500;

            DrawingRect dialogPosition = new(width / 2 - dialogWidth / 2,
                    height / 2 - dialogHeight / 2,
                    dialogWidth,
                    dialogHeight);

            var textPaint = new DrawingTextInfo
            {
                Color = new DrawingColor(0xFF111111),
                TextSize = 32,
                Alignment = DrawingTextAlignment.Left,
                Typeface = InstallerDrawingManager.Default.UITypeface
            };

            var selectImage = drawingApi.CreateImage("Assets/ArrowRight.png");
            DrawingRect imagePos = new(dialogPosition.X + 15, dialogPosition.Y, selectImage.Width, selectImage.Width);

            // Show Both Options
            if (showFactoryConfirm == showAppConfirm)
            {
                drawingApi.DrawText($"Reset Application Data?", imagePos.X + imagePos.Width + 10, imagePos.Y + 43, textPaint);
                InstallerDrawingManager.Default.AddAction(app, imagePos, selectImage, DrawingColors.SkyBlue);

                dialogPosition.Offset(0, selectImage.Height + 20);
                imagePos = new DrawingRect(dialogPosition.X + 15, dialogPosition.Y, selectImage.Width, selectImage.Width);

                drawingApi.DrawText($"Reset All Data?", imagePos.X + imagePos.Width + 10, imagePos.Y + 43, textPaint);
                InstallerDrawingManager.Default.AddAction(factory, imagePos, selectImage, DrawingColors.WhiteSmoke);
            }
            else
            {
                if (showAppConfirm)
                {
                    drawingApi.DrawText($"Delete App Data - Are you sure?", imagePos.X + imagePos.Width + 10, imagePos.Y + 43, textPaint);
                    InstallerDrawingManager.Default.AddAction(confirmApp, imagePos, selectImage, DrawingColors.SkyBlue);
                }
                if (showFactoryConfirm)
                {
                    drawingApi.DrawText($"Delete All Data - Are you sure?", imagePos.X + imagePos.Width + 10, imagePos.Y + 43, textPaint);
                    InstallerDrawingManager.Default.AddAction(confirmfactory, imagePos, selectImage, DrawingColors.SkyBlue);
                }
            }

            dialogPosition.Offset(0, selectImage.Height + 20);
            imagePos = new DrawingRect(dialogPosition.X + 15, dialogPosition.Y, selectImage.Width, selectImage.Width);
            drawingApi.DrawText($"Cancel Reset?", imagePos.X + imagePos.Width + 10, imagePos.Y + 43, textPaint);
            var cancelImage = drawingApi.CreateImage("Assets/exit_icon.png");
            InstallerDrawingManager.Default.AddAction(cancel_reset, imagePos, cancelImage, DrawingColors.WhiteSmoke);

        }
        else
        {
            var textInfo = new DrawingTextInfo
            {
                Color = new DrawingColor(0xFF111111),
                TextSize = 40,
                Alignment = DrawingTextAlignment.Center,
                Typeface = InstallerDrawingManager.Default.UITypeface
            };

            drawingApi.DrawText($"Starting Services...", width / 2, height / 2, textInfo);
        }

    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        if (action != "Refresh")
            timer.Stop();

        if (action == usb)
            return new LocalFileWaitingState(true);
        else if (action == ethernet)
            return new LocalFileWaitingState();
        else if (action == cloud)
            return new WiFiSetupState();
        else if (action == shutdown)
            InstallerDrawingManager.Default.Close();
        else if (action == "?")
            showHelp = true;
        else if (action == reset)
            showResetOptions = true;
        else if (action == factory)
        {
            showFactoryConfirm = true;
            showAppConfirm = false;
        }
        else if (isDeleting)
            return new CompletedInstallState(new InstallCompleteInfo() { Message = $"Finished Delete App: {showFactoryConfirm} Full: {showFactoryConfirm}" });
        else if (action == app)
        {
            showAppConfirm = true;
            showFactoryConfirm = false;
        }
        else if (action == confirmApp)
        {
            // Delete App Data and Got to End State
            isDeleting = true;
        }
        else if (action == confirmfactory)
        {
            isDeleting = true;
        }
        else if (action == cancel_reset)
            showAppConfirm = showFactoryConfirm = showResetOptions = false;
        else if (action == "Support")
        {
            timer.Enabled = false;
            showHelp = false;
        }

        return this;
    }
}
