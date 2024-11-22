using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Utility;
using Ahsoka.ServiceFramework;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;
using TouchEventArgs = Ahsoka.Core.Drawing.Utility.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Platform;

internal class LinuxWindow : IDrawingWindow
{
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
        size = new(0,0);

        InitNativeWindow(apiWindow, appid, surfaceId);
        return InitDrawingApi(apiWindow, (int)size.Width, (int)size.Height);
    }

    IDrawingApi IDrawingWindow.CreateWindow(DrawingWindow apiWindow, bool fullScreen, int width, int height, int surfaceId, string appid)
    {
        this.isFullScreen = fullScreen;
        this.size = new DrawingSize(width,height);

        InitNativeWindow(apiWindow, appid, surfaceId);
        return InitDrawingApi(apiWindow, (int)size.Width, (int)size.Height);
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

    private void InitNativeWindow(DrawingWindow apiWindow, string appId, int surfaceId)
    {
        this.drawingWindow = apiWindow;
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Creating Linux Window - {appId}...");

        int windowResult = 0;
        
        if ((size.Width > 0 && size.Height > 0) || isFullScreen)
            windowResult = impl.InitNativeWindowWithSize(this.isFullScreen, (int)size.Width,(int) size.Height, appId, surfaceId);
        else
            windowResult = impl.InitNativeWindowWithSurfaceId(surfaceId, appId);

        if (windowResult != 0)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Failed to init Wayland Window, exiting...");
            return;
        }

        int boundsHeight = 0, boundsWidth = 0;
        impl.GetWindowInformation(ref boundsWidth, ref boundsHeight);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium,$"Active Window Dimensions: w:[{boundsWidth}] h:[{boundsHeight}]");

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
    }

    int IDrawingWindow.ChangeLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges)
    {
        return impl.ChangeLocation(surfaceId, x, y, w, h, commitChanges);
    }

    int IDrawingWindow.ChangeSource(int surfaceId, int x, int y, int w, int h, bool commitChanges)
    {
        return impl.ChangeSource(surfaceId, x, y, w, h, commitChanges);
    }

    int IDrawingWindow.ChangeVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges)
    {
        return impl.ChangeVisbility(surfaceId, isVisible, visibility, commitChanges);
    }

    void IDrawingWindow.Close()
    {
        // Request Close of Window / Touch
        cancelTokenSource.Cancel();
        touchHandler?.Wait();

        // Add destroyContext() call to clean up and reclaim weston / egl memory
        impl.DestroyContext();
    }

    public void ShowAndRun()
    {
        // Main Run Loop
        while(!cancelTokenSource.IsCancellationRequested)
        {
            while (eventQueue.Count > 0)
                eventQueue.Take().Invoke(this,EventArgs.Empty);
           
            // Reset Context
            if (drawingWindow.HandleFrame())
                context.Flush(true, false);
            
            impl.SwapBuffers();
        }
    }

    void IDrawingWindow.HandleEvent(EventHandler handler)
    {
        eventQueue.Add(handler);
    }
}

public abstract class LinuxDrawingImplementation
{
    internal abstract int InitNativeWindowWithSize(bool fullscreen, int width, int height, string appid, int surfaceId);

    internal abstract int InitNativeWindowWithSurfaceId(int surfaceId, string appid);

    internal abstract void SwapBuffers();

    internal abstract bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000);

    internal abstract void GetWindowInformation(ref int w, ref int h);

    internal abstract void GetSurfaceInformation(ref int sampleCount, ref int stencilBits, ref uint gl_format);
    
    internal abstract nint GetFunctionAddress(string name);

    internal abstract void SetSwapInterval(int interval);

    internal abstract void DestroyContext();

    internal abstract int ChangeLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);

    internal abstract int ChangeSource(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);

    internal abstract int ChangeVisbility(int surfaceId, bool isVisible, float visibility, bool commitChanges = true);
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
