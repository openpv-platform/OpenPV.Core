using Ahsoka.Core.Drawing.Base;
using Gdk;
using System.Diagnostics;
using System.Reflection;
using Application = Gtk.Application;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;
using Window = Gtk.Window;

namespace Ahsoka.Core.Drawing.Cairo;

internal class DesktopCairoWindow : ICairoWindow
{
    // Windows
    Window window;

    void ICairoWindow.Close()
    {
        window.Close();
        window.Dispose();
        Application.Quit();
    }

    void ICairoWindow.RequestDraw(DrawingRect bounds)
    {
        window.QueueDraw();
    }

    void ICairoWindow.ShowAndRun()
    {
        // This will Block
        window.Show();
        Application.Run();
    }

    void ICairoWindow.CreateWindow(CairoDrawingWindow apiWindow, ref DrawingRect bounds, bool fullScreen)
    {
        int height = (int)bounds.Height;
        int width = (int)bounds.Width;

        Assembly assembly = Assembly.GetEntryAssembly();
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        var productName = fvi.ProductName;

        Gtk.Application.Init();
        window = new Window(productName);
        window.SetDefaultSize((int)bounds.Width, (int)bounds.Height);
        window.Events = EventMask.LeaveNotifyMask | EventMask.ButtonPressMask
            | EventMask.TouchMask
            | EventMask.ButtonReleaseMask;

        window.ButtonPressEvent += (o, args) =>
        {
            var touch = new TouchEventArgs()
            {
                Event = TouchEvent.Pressed,
                X = (int)args.Event.X,
                Y = (int)args.Event.Y,
                Index = 1
            };

            apiWindow.RaiseTouchEvent(touch);

            touch.Handled = true;
        };

        window.ButtonReleaseEvent += (o, args) =>
        {
            var touch = new TouchEventArgs()
            {
                Event = TouchEvent.Released,
                X = (int)args.Event.X,
                Y = (int)args.Event.Y,
                Index = 1
            };

            apiWindow.RaiseTouchEvent(touch);

            touch.Handled = true;
        };

        window.Drawn += (o, arg) =>
        {
            // Draw Frames on Windows
            var frameArgs = new CairoDrawFrameArgs()
            {
                Context = arg.Cr,
                Height = height,
                Width = width
            };

            apiWindow.RaiseDrawEvent(frameArgs);
        };
    }

    void ICairoWindow.QueueInvoke(EventHandler handler)
    {
        // Invoke in GTI Application Loop.
        Gtk.Application.Invoke(handler);
    }
}

