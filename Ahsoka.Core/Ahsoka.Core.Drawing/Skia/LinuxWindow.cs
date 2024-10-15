using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Skia;
using Ahsoka.ServiceFramework;
using SkiaSharp;
using System.Collections.Concurrent;
using Task = System.Threading.Tasks.Task;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Skia;

internal class LinuxWindow : IDrawingWindow
{
    DrawingImplementation impl;
    int sampleCount = 0;
    int stencilBits = 0;
    uint gl_format = 0x8058;
    DrawingRect windowSize;

    readonly CancellationTokenSource cancelTokenSource = new();
    readonly BlockingCollection<object> eventQueue = new();
    SkiaDrawingWindow apiWindow;
    SKSurface surface;
    GRContext context;

    // Embedded Context
    Task touchHandler;

    protected LinuxWindow(DrawingImplementation impl)
    {
        this.impl = impl;
    }

    void IDrawingWindow.CreateWindow(SkiaDrawingWindow apiWindow, ref DrawingRect rect, bool fullScreen)
    {
        InitWindow(apiWindow, ref rect, fullScreen);

        try
        {
            touchHandler = Task.Run(() =>
            {
                TouchData data = new();
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (impl.UpdateTouchData(ref data, 1000))
                    {
                        eventQueue.Add(new TouchEventArgs()
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
    }

    private void InitWindow(SkiaDrawingWindow apiWindow, ref DrawingRect rect, bool fullScreen)
    {
        this.apiWindow = apiWindow;

        int i = impl.InitWindow(0, 0, (int)rect.Width, (int)rect.Height, fullScreen);
        if (i != 0)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Failed to init Wayland Window, exiting...");
            return;
        }

        int boundsHeight = 0, boundsWidth = 0;
        impl.GetWindowInformation(ref boundsWidth, ref boundsHeight);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium,$"Active Window Dimensions: w:[{boundsWidth}] h:[{boundsHeight}]");

        rect.Width = boundsWidth;
        rect.Height = boundsHeight;
        windowSize = rect;

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

    void IDrawingWindow.QueueInvoke(EventHandler handler)
    {
        eventQueue.Add(handler);
    }

    void IDrawingWindow.RequestDraw(DrawingRect bounds)
    {
        eventQueue.Add(new DrawFrameArgs()
        {
            Surface = surface,
            Height = (int)bounds.Height,
            Width = (int)bounds.Width
        });
    }

    public void ShowAndRun()
    {
        // Show (First Draw)
        apiWindow.RequestDraw();

        try
        {
            while (eventQueue.TryTake(out object messageData, -1, cancelTokenSource.Token))
            {
                if (messageData is TouchEventArgs touchData)
                {
                    apiWindow.RaiseTouchEvent(touchData);

                    touchData.Handled = true;
                }
                else if (messageData is DrawFrameArgs frameData)
                {
                    // Reset Context
                    apiWindow.RaiseDrawEvent(frameData);

                    context.Flush(true, true);

                    // GL Swap Buffers
                    impl.SwapBuffers();
                }
                else if (messageData is EventHandler handler)
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}

public abstract class DrawingImplementation
{
    internal abstract int InitWindow(int x, int y, int w, int h, bool fullscreen);

    internal abstract void SwapBuffers();

    internal abstract bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000);

    internal abstract void GetWindowInformation(ref int w, ref int h);

    internal abstract void GetSurfaceInformation(ref int sampleCount, ref int stencilBits, ref uint gl_format);
    
    internal abstract nint GetFunctionAddress(string name);

    internal abstract void SetSwapInterval(int interval);

    internal abstract void DestroyContext();
}

