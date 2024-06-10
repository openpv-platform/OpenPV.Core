using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Cairo;
using Ahsoka.Dispatch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.CS.BLE;

internal class MainUI
{
    // Const Colors for our Drawing
    readonly DrawingColor backgroundColor = new(0xFF4682B4);
    readonly DrawingColor colorFill = new(0xFFFFFFFF);
    readonly DrawingColor colorFillTouched = new(0xFF444444);
    readonly DrawingColor colorStroke = new(0xFF111111);
    DrawingTextInfo defaultTextInfo;
    bool isReady = false;
    string startupString = "Initializing System";
    CairoDrawingWindow window;

    bool pairingShown = false;
    readonly Dictionary<string, string> statusTextAreas = new();
    readonly List<TouchArea> touchAreas = new();

    public void RedrawScreen()
    {
        //Normally we would only draw if there is a change to our model or data
        // however here we are simply drawing at max framerate to bound our ball around
        // the screen similar to many game loops
        window.RequestDraw();
    }

    public void InitStateChanged(bool isReady, string startupString)
    {
        this.isReady = isReady;
        this.startupString = startupString;
        RedrawScreen();
    }

    public CairoDrawingWindow StartUI()
    {
        // Create Window
        window = new CairoDrawingWindow(OperatingSystem.IsLinux());

        // Create the Ahsoka Cairo Drawing API and matching Window
        CairoDrawingApi api = new(window.Bounds);

        // This is our screen bounds
        DrawingRect bounds = window.Bounds;

        // Load our Font.  These can be used for any Text Infos your app creates
        // and disposed when no longer used.
        var typeface = api.LoadTypeface("Font.ttf", "Droid Sans");

        // Create a Text Info to describe our font / text attributes
        defaultTextInfo = new DrawingTextInfo()
        {
            Alignment = DrawingTextAlignment.Center,
            Color = backgroundColor,
            TextSize = 28,
            Typeface = typeface
        };

        // Close Button Bounds
        var closeButton = new DrawingRect(bounds.Width - 60, 15, 45, 45);
        touchAreas.Add(new TouchArea() { Text = "X", Command = CloseWindow, IsTouched = false, Rect = closeButton });

        // Handle the Touch Recieved Event
        // here we will simply change the color of the background on the touch
        window.TouchReceived += (o, args) =>
        {
            int x = args.X;
            int y = args.Y;
            api.DeviceToScreen(ref x, ref y);

            if (args.Event == TouchEvent.Pressed)
            {
                foreach (var area in touchAreas)
                    area.IsTouched = area.Rect.Contains(x, y);

                RedrawScreen();
            }
            else if (args.Event == TouchEvent.Released)
            {
                foreach (var area in touchAreas)
                {
                    if (area.Rect.Contains(x, y) && area.IsTouched)
                        Dispatcher.Default.InvokeDispatcher(area.Command);

                    area.IsTouched = false;
                }
                RedrawScreen();
            }
        };

        // Handle the Draw Frame.
        window.DrawFrame += (o, args) =>
        {
            // Initialize the API for each Frame
            api.StartFrame(args.Context);

            DrawBackground(api, bounds);

            if (isReady)
            {
                DrawStatus(api, bounds);

                if (pairingShown)
                {
                    var acceptButton = new DrawingRect(window.Bounds.Width / 2 - 140, window.Bounds.Height / 2 - 140, 280, 290);
                    api.DrawRectangle(acceptButton, colorStroke, DrawingColors.Gainsboro, 2);
                }

                DrawButtons(api, bounds);
            }

            api.EndFrame();
        };
        return window;
    }

    private void CloseWindow()
    {
        window.Close();
        Dispatcher.Default.Stop();
    }

    public void AddButton(Action action, string buttonText)
    {
        var drawingRect = new DrawingRect(20, 95 + ((touchAreas.Count - 1) * 130), 200, 45);
        touchAreas.Add(new TouchArea() { Text = buttonText, Command = action, Rect = drawingRect });
    }

    public void UpdateStatusText(string titleText, string statusText)
    {
        statusTextAreas[titleText] = statusText;
    }

    private void DrawBackground(CairoDrawingApi api, DrawingRect bounds)
    {
        // Clear Background
        api.Clear(backgroundColor);
        api.DrawRectangle(new DrawingRect(0, 0, bounds.Width, 75), colorFill, colorFill, 0);

        // Draw the current time in our circle
        defaultTextInfo.Color = backgroundColor;
        defaultTextInfo.TextSize = 28;
        defaultTextInfo.Alignment = DrawingTextAlignment.Left;
        api.DrawText("OpenPV 1.0 Bluetooth Demo", 10, 35, defaultTextInfo);

        defaultTextInfo.TextSize = 16;
        api.DrawText(startupString, 15, 60, defaultTextInfo);
    }

    private void DrawStatus(CairoDrawingApi api, DrawingRect bounds)
    {
        var drawingRect = new DrawingRect(250, 124, 100, 25);

        defaultTextInfo.TextSize = 18;
        defaultTextInfo.Color = colorFill;
        defaultTextInfo.Alignment = DrawingTextAlignment.Left;
        foreach (var item in statusTextAreas)
        {
            api.DrawText(item.Key, drawingRect.X, drawingRect.Y, defaultTextInfo);

            defaultTextInfo.TextWeight = DrawingTextWeight.Italic;
            api.DrawText(item.Value, drawingRect.X + 250, drawingRect.Y, defaultTextInfo);

            drawingRect.Offset(0, 65);
        }

        defaultTextInfo.TextWeight = DrawingTextWeight.Normal;

    }

    private void DrawButtons(CairoDrawingApi api, DrawingRect bounds)
    {
        defaultTextInfo.Color = backgroundColor;
        defaultTextInfo.TextSize = 18;
        defaultTextInfo.Alignment = DrawingTextAlignment.Center;
        foreach (var item in touchAreas)
        {
            if (item.IsTouched)
                api.DrawRectangle(item.Rect, colorStroke, colorFillTouched, 1);
            else
                api.DrawRectangle(item.Rect, colorStroke, colorFill, 1);

            api.DrawText(item.Text, item.Rect.X + item.Rect.Width / 2, item.Rect.Y + item.Rect.Height / 2 + 7, defaultTextInfo);
        }
    }

    internal void ShowPairingRequest(string passcode, Action pairAccepted, Action pairRejected)
    {
        pairingShown = true;
        var acceptButton = new DrawingRect(window.Bounds.Width / 2 - 120, window.Bounds.Height / 2 - 120, 240, 120);
        touchAreas.Add(new TouchArea() { Text = $"Accept Pairing: {passcode}", Command = pairAccepted, IsTouched = false, Rect = acceptButton });

        var rejectButton = new DrawingRect(acceptButton.X, acceptButton.Y + acceptButton.Height + 10, 240, 120);
        touchAreas.Add(new TouchArea() { Text = $"Reject Pairing", Command = pairRejected, IsTouched = false, Rect = rejectButton });

        RedrawScreen();
    }

    internal void HidePairingRequest()
    {
        pairingShown = false;
        touchAreas.Remove(touchAreas.Last());
        touchAreas.Remove(touchAreas.Last());

        RedrawScreen();
    }

    class TouchArea
    {
        public DrawingRect Rect;
        public bool IsTouched;
        public Action Command;
        public string Text;
    }
}
