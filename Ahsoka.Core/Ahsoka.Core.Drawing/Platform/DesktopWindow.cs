using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Utility;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;
using System.Diagnostics;
using System.Reflection;

namespace Ahsoka.Core.Drawing.Platform;

internal class DesktopWindow : IDrawingWindow
{
    // Windows
    private IWindow window;
    SKSurface surface;
    GRContext context;
    DrawingRect drawingRect;
    GL gl;

    IDrawingApi IDrawingWindow.CreateWindow(DrawingWindow apiWindow, DrawingRect bounds, bool fullScreen, int surfaceId, string appid)
    {
        drawingRect = bounds;

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)bounds.Width, (int)bounds.Height);
        options.Title = appid;
        options.IsContextControlDisabled = true;

        return CreateWindow(apiWindow, options);
    }

    IDrawingApi IDrawingWindow.CreateWindow(DrawingWindow apiWindow, int surfaceId, string appid)
    {

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1024, 600);
        options.Title = appid;
        options.IsContextControlDisabled = true;

        drawingRect = new DrawingRect(0,0,options.Size.X, options.Size.Y);

        return CreateWindow(apiWindow, options);
    }

    private IDrawingApi CreateWindow(DrawingWindow apiWindow, WindowOptions options)
    {
        window = Silk.NET.Windowing.Window.Create(options);
        window.Load += () =>
        {
            gl = window.CreateOpenGL();
            window.GLContext.SwapInterval(1);

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

                        apiWindow.AddTouchEvent(touch);
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

                        apiWindow.AddTouchEvent(touch);
                        touch.Handled = true;
                    }
                };
            }
        };


        window.Render += (deltaTime) =>
        {

            context.ResetContext();

            // Handle the Frame (Prepare, Render) returns if New Render was Don
            if (apiWindow.HandleFrame())
                context?.Flush(true, true);

            window.GLContext.SwapBuffers();

            // Copy To Front to Back
            // (Tech Debt) No Support for Preserver Buffers needed for Drawing Rectangles
            gl.ReadBuffer(GLEnum.Front);
            gl.DrawBuffer(GLEnum.Back);
            gl.Disable(GLEnum.ScissorTest);
            gl.BlitFramebuffer(0, 0, (int)drawingRect.Width, (int)drawingRect.Height,
                               0, 0, (int)drawingRect.Width, (int)drawingRect.Height, (int)GLEnum.ColorBufferBit, GLEnum.Nearest);
        };

        window.Initialize();

        InitWindow();

        return new SkiaDrawingApi(new DrawingSize( drawingRect.Width,drawingRect.Height), surface);
    }

    private void InitWindow()
    {
        var grGlInterface = GRGlInterface.CreateOpenGl((name) =>
        {
            if (window.GLContext.TryGetProcAddress(name, out var gl))
                return gl;
            else
                return 0;
        });

        context = GRContext.CreateGl(grGlInterface,new GRContextOptions() { });
        var renderTarget = new GRBackendRenderTarget(window.Size.X, window.Size.Y, 0, 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));
        surface = SKSurface.Create(context, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
    }

    void IDrawingWindow.Close()
    {
        window.Close();
        surface.Dispose();
        context.Dispose();
        surface = null;
        context = null;
        window = null;
    }

    void IDrawingWindow.ShowAndRun()
    {
        // This will Block
        window.Run();
    }

    void IDrawingWindow.HandleEvent(EventHandler handler)
    {
        handler.Invoke(this, EventArgs.Empty);
    }

    int IDrawingWindow.ChangeLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges)
    {
        Console.WriteLine("Size Updates Only supported on Embedded Targets");
        return 0;
    }

    int IDrawingWindow.ChangeSource(int surfaceId, int x, int y, int w, int h, bool commitChanges)
    {
        Console.WriteLine("Source Updates Only supported on Embedded Targets");
        return 0;
    }

    int IDrawingWindow.ChangeVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges)
    {
        Console.WriteLine("Visibility Updates Only supported on Embedded Targets");
        return 0;
    }
}

