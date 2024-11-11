using Ahsoka.ServiceFramework;
using Ahsoka.System;
using SkiaSharp;
using DateTime = System.DateTime;
using TouchEventArgs = Ahsoka.Core.Drawing.Utility.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Base;

/// <summary>
/// Class to manage an Accelerated Window and Drawing Context
/// </summary>
public class DrawingWindow
{
    #region Fields
    bool firstFrame  = true;
    int frameCount = 0;
    const int frameSeconds = 10;
    DateTime nextPrintTime = DateTime.Now.AddSeconds(frameSeconds);
    readonly DrawingSize size;
    readonly IDrawingWindow nativeWindow;
    readonly SkiaDrawingApi api;

    private readonly object eventLock = new(); // Move to Lock in 9.0
    List<TouchEventArgs> eventList = new();
    #endregion

    #region Events
    public event EventHandler<DrawFrameArgs> PrepareFrame;
    public event EventHandler<DrawFrameArgs> RenderFrame;
    #endregion

    #region Properties
    public IDrawingApi GetApi()
    {
        return api;
    }

    public SkiaDrawingApi GetSkiaApi()
    {
        return api;
    }

    public DrawingSize Size { get => size; }

    public bool FrameRateEnabled { get; set; } = true;
    public bool ShowClipRectangles { get; set; } = true;


    #endregion

    #region Methods
    public DrawingWindow(string appId = "DrawingWindow", bool fullScreen = false, int width = 800, int height = 480)
    {
        size = new (width, height);

        if (SystemInfo.CurrentPlatform is PlatformFamily.Windows64 or PlatformFamily.MacosArm64 or PlatformFamily.Ubuntu64)
        {
            nativeWindow = new Platform.DesktopWindow();
            api = nativeWindow.CreateWindow(this, size, fullScreen, appId) as SkiaDrawingApi;
        }
        else if (SystemInfo.CurrentPlatform == PlatformFamily.OpenViewLinux)
        {
            nativeWindow = new Platform.STWindow();
            api = nativeWindow.CreateWindow(this, size, fullScreen, appId) as SkiaDrawingApi;
        }
        else if (SystemInfo.CurrentPlatform == PlatformFamily.OpenViewLinuxPro)
        {
            nativeWindow = new Platform.TIWindow();
            api = nativeWindow.CreateWindow(this, size, fullScreen, appId) as SkiaDrawingApi;
        }
    }

    public void Close()
    {
        nativeWindow.Close();
    }

    private void PrintFrameCount()
    {
        frameCount++;

        if (FrameRateEnabled && nextPrintTime < DateTime.Now)
        {
            nextPrintTime = DateTime.Now.AddSeconds(frameSeconds);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Framerate {frameCount}: {frameCount / frameSeconds} ");
            frameCount = 0;
        }
    }

    public void ShowAndRun()
    {
        nativeWindow.ShowAndRun();
    }

    internal void AddTouchEvent(TouchEventArgs touchData)
    {
        lock (eventLock)
            eventList.Add(touchData);
    }

    SKPaint paint = new SKPaint() { Color = DrawingColors.Red.AsSKColor(), IsStroke = true, StrokeWidth = 3 };

    internal bool HandleFrame()
    {
        DrawFrameArgs frameArgs = null;

        // Prepare Frame Args
        lock (eventLock)
        {
            frameArgs = api.StartFrame();
            frameArgs.Events = eventList;
            eventList = new();
        }

        PrepareFrame?.Invoke(this, frameArgs);

        // Render First Frame Always
        if (firstFrame)
        {
            frameArgs.ShouldClip = false; 
            frameArgs.ShouldRender = true;
            frameArgs.ClipRegions.Clear();
        }

        if (frameArgs.ShouldRender)
        {
            // Push Frame Clipping Rectangles
            if (frameArgs.ShouldClip)
                api.PushClip(frameArgs.ClipRegions);

            RenderFrame?.Invoke(this, frameArgs);

            if (frameArgs.ShouldClip)
                api.PopClip();

            firstFrame = false;
        }

        api.FinishFrame(frameArgs);

        PrintFrameCount();

        return frameArgs.ShouldRender;
    }

    public void Invoke(EventHandler handler)
    {
        nativeWindow.HandleEvent(handler);
    }

    #endregion
}


