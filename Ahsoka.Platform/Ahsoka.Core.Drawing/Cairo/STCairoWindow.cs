using Ahsoka.Core.Drawing.Base;
using Cairo;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Context = Cairo.Context;
using Task = System.Threading.Tasks.Task;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Cairo;

internal class STCairoWindow : ICairoWindow
{
    #region Native Interop
    public const string NativeLibraryName = "AhsokaLib/Ahsoka.Cairo.GLES.so";

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int InitWindowInterop(int x, int y, int w, int h, bool fullscreen);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr GetCairoSurface();

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GLSwapBuffers(IntPtr p);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool GetTouchData(ref TouchData data, int timeoutInMs = 1000);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GetWindowDimensions(ref int w, ref int h);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void DestoryWindowContext();

    #endregion

    readonly CancellationTokenSource cancelTokenSource = new();
    readonly BlockingCollection<object> eventQueue = new();
    CairoDrawingWindow apiWindow;

    // Embedded Context
    private IntPtr cairoSurface;
    private OpenPVSurface surface;
    Context cairoContext;
    Task touchHandler;

    void ICairoWindow.CreateWindow(CairoDrawingWindow apiWindow, ref DrawingRect rect, bool fullScreen)
    {
        this.apiWindow = apiWindow;

        int width = (int)rect.Width;
        int height = (int)rect.Height;

        int i = InitWindowInterop(0, 0, width, height, fullScreen);
        if (i != 0)
        {
            Console.WriteLine("Failed to init Wayland Window, exiting...");
            return;
        }

        int boundsHeight = 0, boundsWidth = 0;
        GetWindowDimensions(ref boundsWidth, ref boundsHeight);
        Console.WriteLine($"Active Window Dimensions: w:[{boundsWidth}] h:[{boundsHeight}]");

        rect.Width = boundsWidth;
        rect.Height = boundsHeight;

        // Create Cairo Window / Surface
        cairoSurface = GetCairoSurface();

        // Create CairoSharp Surface adn Context
        surface = new OpenPVSurface(cairoSurface);
        cairoContext = new Context(surface);

        try
        {

            touchHandler = Task.Run(() =>
            {
                TouchData data = new();
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (GetTouchData(ref data, 1000))
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

    void ICairoWindow.Close()
    {
        // Request Close of Window / Touch
        cancelTokenSource.Cancel();
        touchHandler?.Wait();

        // TODO: Add destroyContext() call to clean up and reclaim weston / egl / cairo memory

    }

    void ICairoWindow.QueueInvoke(EventHandler handler)
    {
        eventQueue.Add(handler);
    }

    void ICairoWindow.RequestDraw(DrawingRect bounds)
    {
        eventQueue.Add(new CairoDrawFrameArgs()
        {
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
                else if (messageData is CairoDrawFrameArgs frameData)
                {
                    // Set Context
                    frameData.Context = cairoContext;
                    apiWindow.RaiseDrawEvent(frameData);
                    GLSwapBuffers(cairoSurface);
                }
                else if (messageData is EventHandler handler)
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    internal class OpenPVSurface : Surface
    {
        internal OpenPVSurface(IntPtr surface) :
            base(surface, false)
        {

        }
    }
}

