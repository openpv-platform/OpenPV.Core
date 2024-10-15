using Ahsoka.Core.Drawing.Base;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using SkiaSharp;
using System.Runtime.InteropServices;
using DateTime = System.DateTime;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Skia;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    void Close();
    void CreateWindow(SkiaDrawingWindow apiWindow, ref DrawingRect bounds, bool fullScreen);
    void QueueInvoke(EventHandler handler);
    void RequestDraw(DrawingRect bounds);
    void ShowAndRun();
}

/// <summary>
/// Class to manage an Accelerated Window and Drawing Context
/// </summary>
public class SkiaDrawingWindow
{
    #region Fields
    int frameCount = 0;
    const int frameSeconds = 10;
    DateTime nextPrintTime = DateTime.Now.AddSeconds(frameSeconds);
    readonly DrawingRect bounds;
    readonly IDrawingWindow nativeWindow;
    readonly IDrawingApi api;
    #endregion

    #region Events
    public event EventHandler<TouchEventArgs> TouchReceived;
    public event EventHandler<DrawFrameArgs> DrawFrame;
    #endregion

    #region Properties
    public IDrawingApi GetApi() 
    { 
        return api; 
    }

    public DrawingRect Bounds { get => bounds; }
    #endregion

    #region Methods
    public SkiaDrawingWindow(bool fullScreen = false, int x = 0, int y = 0, int width = 800, int height = 480)
    {
        bounds = new DrawingRect(x, y, width, height);

        if (SystemInfo.CurrentPlatform is PlatformFamily.Windows64 or
            PlatformFamily.MacosArm64)
        {
            nativeWindow = new DesktopWindow();
            nativeWindow.CreateWindow(this, ref bounds, fullScreen);
        }
        else if (SystemInfo.CurrentPlatform == PlatformFamily.OpenViewLinux) 
        {
            nativeWindow = new STWindow();
            nativeWindow.CreateWindow(this, ref bounds, fullScreen);
        }
        else if (SystemInfo.CurrentPlatform == PlatformFamily.OpenViewLinuxPro)
        {
            nativeWindow = new TIWindow();
            nativeWindow.CreateWindow(this, ref bounds, fullScreen);
        }

        api = new SkiaDrawingApi(bounds) ;
    }

    public void Close()
    {
        nativeWindow.Close();
    }

    public void RequestDraw()
    {
        nativeWindow.RequestDraw(bounds);
    }

    private void PrintFrameCount()
    {
        frameCount++;

        if (nextPrintTime < DateTime.Now)
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

    internal void RaiseTouchEvent(TouchEventArgs touchData)
    {
        TouchReceived?.Invoke(this, touchData);
    }

    internal void RaiseDrawEvent(DrawFrameArgs drawFrameData)
    {
        api.StartFrame(drawFrameData);
        DrawFrame?.Invoke(this, drawFrameData);
        api.FinishFrame();
        PrintFrameCount();
    }


    public void Invoke(EventHandler handler)
    {
        nativeWindow.QueueInvoke(handler);
    }

    #endregion
}

public class DrawFrameArgs
{
    public SKSurface Surface { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct TouchData
{
    [FieldOffset(0)]
    public TouchEvent Event;

    [FieldOffset(4)]
    public int X;

    [FieldOffset(8)]
    public int Y;

    [FieldOffset(12)]
    public int Index;
}

