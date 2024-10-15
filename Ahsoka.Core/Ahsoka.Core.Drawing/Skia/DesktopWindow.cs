using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Skia;
using GLib;
using Gst;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;
using System.Diagnostics;
using System.Reflection;

namespace Ahsoka.Core.Drawing.Skia;

internal class DesktopWindow : IDrawingWindow
{
    // Windows
    private IWindow window;
    SKSurface surface;

    void IDrawingWindow.Close()
    {
        //window.Close();
        //window.Dispose();
        //Application.Quit();
    }

    AutoResetEvent eventItem = new(false);
    void IDrawingWindow.RequestDraw(DrawingRect bounds)
    {
       
    }

    void IDrawingWindow.ShowAndRun()
    {
        // This will Block
        window.Run();
    }

    void IDrawingWindow.CreateWindow(SkiaDrawingWindow apiWindow, ref DrawingRect bounds, bool fullScreen)
    {
        int height = (int)bounds.Height;
        int width = (int)bounds.Width;

        Assembly assembly = Assembly.GetEntryAssembly();
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        var productName = fvi.ProductName;

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)bounds.Width, (int)bounds.Height);
        options.Title = "OpenPV Drawing";

        GRContext context = null;

        window = Window.Create(options);
        window.Load += () =>
        {
            var grGlInterface = GRGlInterface.CreateOpenGl((name) =>
            {
                if (window.GLContext.TryGetProcAddress(name, out var gl))
                    return gl;
                else
                    return 0;
            });


            context = GRContext.CreateGl(grGlInterface);
            var renderTarget = new GRBackendRenderTarget(window.Size.X, window.Size.Y, 0, 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));
            surface = SKSurface.Create(context, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

            IInputContext input = window.CreateInput();
            for (int i = 0; i < input.Mice.Count; i++)
            {
                input.Mice[i].MouseDown += (o, e) =>
                {
                    if (e == MouseButton.Left)
                    {
                        var touch = new TouchEventArgs()
                        {
                            Event = TouchEvent.Pressed,
                            X = (int)o.Position.X,
                            Y = (int)o.Position.Y,
                            Index = 1
                        };

                        apiWindow.RaiseTouchEvent(touch);

                        touch.Handled = true;
                    }
                };

                input.Mice[i].MouseUp += (o, e) =>
                {
                    if (e == MouseButton.Left)
                    {
                        var touch = new TouchEventArgs()
                        {
                            Event = TouchEvent.Released,
                            X = (int)o.Position.X,
                            Y = (int)o.Position.Y,
                            Index = 1
                        };

                        apiWindow.RaiseTouchEvent(touch);

                        touch.Handled = true;
                    }
                };
            }
        };
          
        window.Update += (deltatime) => { };
        window.Render += (deltaTime)=>
        {
            // Draw Frames on Windows
            var frameArgs = new DrawFrameArgs()
            {
                Surface = surface,
                Height = height,
                Width = width
            };

            apiWindow.RaiseDrawEvent(frameArgs);

            context?.Flush(true, true);
        };
    }

    void IDrawingWindow.QueueInvoke(EventHandler handler)
    {
        // Invoke in GTI Application Loop.
        //Gtk.Application.Invoke(handler);
    }
}

