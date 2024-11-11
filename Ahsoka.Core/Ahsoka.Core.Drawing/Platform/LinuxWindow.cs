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
    DrawingSize windowSize;

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

    IDrawingApi IDrawingWindow.CreateWindow(DrawingWindow apiWindow, DrawingSize size, bool fullScreen, string appid)
    {
        InitWindow(apiWindow, size, fullScreen, appid);

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

        return new SkiaDrawingApi(size, surface);
    }

    private void InitWindow(DrawingWindow apiWindow, DrawingSize size, bool fullScreen, string appId)
    {
        this.drawingWindow = apiWindow;
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Creating Linux Window - {appId}...");

        int i = impl.InitWindow(size, fullScreen, appId);
        if (i != 0)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Failed to init Wayland Window, exiting...");
            return;
        }

        int boundsHeight = 0, boundsWidth = 0;
        impl.GetWindowInformation(ref boundsWidth, ref boundsHeight);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium,$"Active Window Dimensions: w:[{boundsWidth}] h:[{boundsHeight}]");

        size.Width = boundsWidth;
        size.Height = boundsHeight;
        windowSize = size;

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

    void IDrawingWindow.Close()
    {
        // Request Close of Window / Touch
        cancelTokenSource.Cancel();
        touchHandler?.Wait();

        // TODO: Add destroyContext() call to clean up and reclaim weston / egl memory
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
    internal abstract int InitWindow(DrawingSize size, bool fullscreen, string appid);

    internal abstract void SwapBuffers();

    internal abstract bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000);

    internal abstract void GetWindowInformation(ref int w, ref int h);

    internal abstract void GetSurfaceInformation(ref int sampleCount, ref int stencilBits, ref uint gl_format);
    
    internal abstract nint GetFunctionAddress(string name);

    internal abstract void SetSwapInterval(int interval);

    internal abstract void DestroyContext();
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
