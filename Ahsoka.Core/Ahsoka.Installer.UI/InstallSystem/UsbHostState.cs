using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System.IO;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class UsbHostState : InstallerUIStateBase
{
    readonly InstallerFileList fileList;
    const string refresh = "RefreshFiles";
    const string spark = "ConnectSpark";
    readonly bool usbRoleSwapped = false;

    public UsbHostState(bool useUsb = false)
    {
        if (useUsb)
        {
            Services.Device.UsbInfo usbInfo = new()
            {
                Mode = Services.Device.UsbMode.Host,
                MountPoint = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder)
            };
            InstallerDrawingManager.Default.DeviceClient.SetUsbInfo(usbInfo);
            usbRoleSwapped = true;
        }

        fileList = new InstallerFileList();
        fileList.FileChanged += (o, e) =>
        {
            InstallerDrawingManager.Default.Invalidate();
        };

        fileList.Listen = true;
    }

    private void ReverseRollSwap()
    {
        if (usbRoleSwapped)
        {
            Services.Device.UsbInfo usbInfo = new()
            {
                Mode = Services.Device.UsbMode.Device,
                MountPoint = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder)
            };
            InstallerDrawingManager.Default.DeviceClient.SetUsbInfo(usbInfo);
        }
    }

    protected override void OnDrawState(DrawFrameArgs args)
    {
        // Check for Dongle
        IDrawingApi drawingApi = args.DrawingApi;
        int width = args.Width;
        int height = args.Height;
        if (!fileList.HasInstallPackage)
        {
            DrawWaitingForUsbFile(drawingApi, width, height);
        }
        else
        {
            DrawSelectFile(drawingApi, width, height);
        }
    }

    private void DrawWaitingForUsbFile(IDrawingApi drawingApi, int width, int height)
    {
        var image = drawingApi.CreateImage("Assets/USBStick.png");

        DrawingRect position = new(width / 2 - image.Width / 2, height / 2 - image.Height / 2, image.Width, image.Height);
        drawingApi.DrawBitmap(image, position);

        var textPaint = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 24,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };


        drawingApi.DrawText($"Waiting for USB Files", width / 2, height * .333f, textPaint);

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);

        textPaint.TextSize = 16;
        if (InstallerDrawingManager.Default.HasCellModem)
            drawingApi.DrawText($"Cellular Device Available", width / 2, height * .666f - 15, textPaint);
        else
            drawingApi.DrawText($"Cellular Device Not Detected", width / 2, height * .666f - 15, textPaint);

        if (InstallerDrawingManager.Default.HasEthernet)
            drawingApi.DrawText($"USB Ethernet Available", width / 2, height * .666f + 10, textPaint);
        else
            drawingApi.DrawText($"USB Ethernet Not Detected", width / 2, height * .666f + 10, textPaint);

        if (InstallerDrawingManager.Default.HasCellModem || InstallerDrawingManager.Default.HasEthernet)
        {
            var cloudImage = drawingApi.CreateImage("Assets/Cloud.png");
            InstallerDrawingManager.Default.AddAction("ConnectSpark", InstallerDrawingManager.Default.GetActionPosition(5, cancel.Width), cloudImage, DrawingColors.LightSkyBlue);
        }
    }

    private void DrawSelectFile(IDrawingApi drawingApi, int width, int height)
    {
        var textPaint = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 35,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"Select File to Load", width / 2, height * .5f - 10, textPaint);

        textPaint.TextSize = 20;

        drawingApi.DrawText($"{Path.GetFileName(fileList.SelectedFile)}", width / 2, height * .5f + 30, textPaint);

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel); ;

        var down = drawingApi.CreateImage("Assets/ArrowDown.png");
        InstallerDrawingManager.Default.AddAction("Down", InstallerDrawingManager.Default.GetActionPosition(3, down.Width), down); ;

        var up = drawingApi.CreateImage("Assets/ArrowUp.png");
        InstallerDrawingManager.Default.AddAction("Up", InstallerDrawingManager.Default.GetActionPosition(2, up.Width), up); ;

        var right = drawingApi.CreateImage("Assets/ArrowRight.png");
        InstallerDrawingManager.Default.AddAction("Select", InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen); ;
    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        if (action == refresh)
            return this;
        else if (action == spark)
        {
            if (InstallerDrawingManager.Default.HasEthernet)
                return new SparkConnectionState() { RestoreState = this };
            else if (InstallerDrawingManager.Default.HasCellModem)
                return new SparkConnectionState() { RestoreState = this };
        }
        else if (action == "Up")
            fileList.MoveUp();
        else if (action == "Down")
            fileList.MoveDown();
        else if (action == "Cancel")
        {
            fileList.Listen = false;
            ReverseRollSwap();
            return new StartState();
        }

        if (action == "Select")
        {
            fileList.Listen = false;
            string tempFile = SystemInfo.HardwareInfo.TargetPathInfo.GetTempFileName();
            File.Copy(fileList.SelectedFile, tempFile);
            ReverseRollSwap();
            return new InstallingState(tempFile);
        }

        return this;
    }
}
