using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Utility;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;
using TouchEventArgs = Ahsoka.Core.Drawing.Utility.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Platform;

internal class LinuxWindow : IDrawingWindow
{
    int surfaceId = 0;
    LinuxDrawingImplementation impl;
    int sampleCount = 0;
    int stencilBits = 0;
    uint gl_format = 0x8058;
    DrawingSize size;
    bool isFullScreen = false;
    readonly CancellationTokenSource cancelTokenSource = new();
    readonly BlockingCollection<EventHandler> eventQueue = new();

    DrawingWindow drawingWindow;
    SKSurface surface;
    GRContext context;


    // Embedded Context
    Task touchHandler;

    protected LinuxWindow(LinuxDrawingImplementation impl)
    {
        this.impl = impl;
    }

    IDrawingApi IDrawingWindow.CreateWindow(DrawingWindow apiWindow, int surfaceId, string appid)
    {
        size = new(0, 0);

        surfaceId = InitNativeWindow(apiWindow, appid, surfaceId);
        return InitDrawingApi(apiWindow, (int)size.Width, (int)size.Height);
    }

    IDrawingApi IDrawingWindow.CreateWindow(DrawingWindow apiWindow, bool fullScreen, int width, int height, string appid)
    {
        this.isFullScreen = fullScreen;
        this.size = new DrawingSize(width, height);

        surfaceId = InitNativeWindow(apiWindow, appid, -1);
        return InitDrawingApi(apiWindow, (int)size.Width, (int)size.Height);
    }

    int IDrawingWindow.GetSurfaceId()
    {
        return surfaceId;
    }

    private IDrawingApi InitDrawingApi(DrawingWindow apiWindow, int width, int height)
    {
        try
        {
            touchHandler = Task.Run(() =>
            {
                TouchData data = new();
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (impl.UpdateTouchData(ref data, 1000))
                    {
                        apiWindow.AddTouchEvent(new TouchEventArgs()
                        {
                            Event = data.Event,
                            Index = data.Index,
                            X = data.X,
                            Y = data.Y,
                            Handled = false
                        });
                    }
                }

            }, cancelTokenSource.Token);
        }
        catch (OperationCanceledException) { }

        return new SkiaDrawingApi(new DrawingSize(width, height), surface);
    }

    private int InitNativeWindow(DrawingWindow apiWindow, string appId, int surfaceId)
    {
        this.drawingWindow = apiWindow;
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Creating Linux Window - {appId}...");

        if (surfaceId == -1 || isFullScreen)
            this.surfaceId = impl.InitNativeWindowWithSize(this.isFullScreen, (int)size.Width, (int)size.Height, appId);
        else
            this.surfaceId = impl.InitNativeWindowWithSurfaceId(surfaceId, appId);

        if (this.surfaceId == -1)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Failed to init Window, exiting...");
            return 0;
        }

        int boundsHeight = 0, boundsWidth = 0;
        impl.GetWindowSize(ref boundsWidth, ref boundsHeight);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Active Window Dimensions: w:[{boundsWidth}] h:[{boundsHeight}]");

        size.Width = boundsWidth;
        size.Height = boundsHeight;

        impl.GetSurfaceInformation(ref sampleCount, ref stencilBits, ref gl_format);
        impl.SetSwapInterval(1);

        // Creates API Access for GLES - WrapsGet
        var value = GRGlInterface.CreateGles(impl.GetFunctionAddress);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Create GLES2 Interface {value != null}");

        context = GRContext.CreateGl(value);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Create EGL Context{context != null}");

        GRBackendRenderTarget renderTarget = new GRBackendRenderTarget(boundsWidth, boundsHeight, sampleCount, stencilBits, new GRGlFramebufferInfo(0, gl_format));
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Create RenderTarget {renderTarget != null}");

        surface = SKSurface.Create(context, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Create Surface {surface != null}");

        return this.surfaceId;
    }


    void IDrawingWindow.Close()
    {

        // Request Close of Window / Touch
        cancelTokenSource.Cancel();
        touchHandler?.Wait(1000);

        // Add destroyContext() call to clean up and reclaim weston / egl memory
        impl.DestroyContext();
    }


    int IDrawingWindow.UpdateLocationRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges)
    {
        return impl.UpdateLocationRectangle(surfaceId, x, y, w, h, commitChanges);
    }

    int IDrawingWindow.UpdateSourceRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges)
    {
        return impl.UpdateSourceRectangle(surfaceId, x, y, w, h, commitChanges);
    }

    int IDrawingWindow.ChangeVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges)
    {
        return impl.UpdateVisibility(surfaceId, isVisible, visibility, commitChanges);
    }

    bool IDrawingWindow.GetSurfaceVisibility(int surfaceId)
    {
        return impl.GetVisibility(surfaceId);
    }

    float IDrawingWindow.GetSurfaceOpacity(int surfaceId)
    {
        return impl.GetOpacity(surfaceId);
    }

    DrawingRect IDrawingWindow.GetSurfaceLocation(int surfaceId)
    {
        return impl.GetLocationRectangle(surfaceId);
    }
    public void ShowAndRun()
    {
        // Listen for Shutdown
        var task = Task.Run(() =>
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "LinuxWindow Waiting for Shutdown");
            ApplicationContext.WaitForExitSignal();
            cancelTokenSource.Cancel();
        }, cancelTokenSource.Token);


        // Main Run Loop
        while (!cancelTokenSource.IsCancellationRequested)
        {
            while (eventQueue.Count > 0)
                eventQueue.Take().Invoke(this,EventArgs.Empty);
           
            // Reset Context
            if (drawingWindow.HandleFrame())
                context.Flush(true, false);
            
            impl.SwapBuffers();
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "LinuxWindow Shutdown Recieved");
    }

    void IDrawingWindow.HandleEvent(EventHandler handler)
    {
        eventQueue.Add(handler);
    }
}

public abstract class LinuxDrawingImplementation
{
    internal abstract int InitNativeWindowWithSize(bool fullscreen, int width, int height, string appid);

    internal abstract int InitNativeWindowWithSurfaceId(int surfaceId, string appid);

    internal abstract void SwapBuffers();

    internal abstract bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000);

    internal abstract void GetWindowSize(ref int w, ref int h);

    internal abstract void GetSurfaceInformation(ref int sampleCount, ref int stencilBits, ref uint gl_format);
    
    internal abstract nint GetFunctionAddress(string name);

    internal abstract void SetSwapInterval(int interval);

    internal abstract void DestroyContext();

    internal abstract int UpdateLocationRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);

    internal abstract int UpdateSourceRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);

    internal abstract int UpdateVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges = true);

    internal abstract bool GetVisibility(int surfaceId);

    internal abstract float GetOpacity(int surfaceId);

    internal abstract DrawingRect GetLocationRectangle(int surfaceId);
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
