using Ahsoka.Core.Drawing.Base;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System.Runtime.InteropServices;
using Context = Cairo.Context;
using DateTime = System.DateTime;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Cairo;

/// <summary>
///  Interface for Cross Plaform Cairo Window Implementations.
/// </summary>
internal interface ICairoWindow
{
    void Close();
    void CreateWindow(CairoDrawingWindow apiWindow, ref DrawingRect bounds, bool fullScreen);
    void QueueInvoke(EventHandler handler);
    void RequestDraw(DrawingRect bounds);
    void ShowAndRun();
}


/// <summary>
/// Class to manage an Accelerated Cairo Window and Drawing Context
/// </summary>
public class CairoDrawingWindow
{
    #region Fields
    int frameCount = 0;
    const int frameSeconds = 10;
    DateTime nextPrintTime = DateTime.Now.AddSeconds(frameSeconds);
    readonly DrawingRect bounds;
    readonly ICairoWindow nativeWindow;
    #endregion

    #region Events
    public event EventHandler<TouchEventArgs> TouchReceived;
    public event EventHandler<CairoDrawFrameArgs> DrawFrame;
    #endregion

    #region Properties
    public DrawingRect Bounds { get => bounds; }
    #endregion

    #region Methods
    public CairoDrawingWindow(bool fullScreen = false, int x = 0, int y = 0, int width = 800, int height = 480)
    {
        bounds = new DrawingRect(x, y, width, height);

        if (SystemInfo.CurrentPlatform is PlatformFamily.Windows64 or
            PlatformFamily.MacosArm64)
        {
            nativeWindow = new DesktopCairoWindow();
            nativeWindow.CreateWindow(this, ref bounds, fullScreen);
        }
        else
        {
            nativeWindow = new STCairoWindow();
            nativeWindow.CreateWindow(this, ref bounds, fullScreen);
        }
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
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Framerate {frameCount}: {frameCount / frameSeconds} ");
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

    internal void RaiseDrawEvent(CairoDrawFrameArgs drawFrameData)
    {
        DrawFrame?.Invoke(this, drawFrameData);

        PrintFrameCount();
    }


    public void Invoke(EventHandler handler)
    {
        nativeWindow.QueueInvoke(handler);
    }


    #endregion
}

public class CairoDrawFrameArgs
{
    public Context Context { get; set; }
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

