using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.Core.Drawing.Base;

    partial class DrawingWindow
    {
        partial void CreateDesktopWindow(int surfaceId, string appId)
        {
            nativeWindow = new Platform.DesktopWindow();
            api = nativeWindow.CreateWindow(this, surfaceId, appId) as SkiaDrawingApi;
        }

        partial void CreateDesktopWindow(bool fullScreen, int width, int height, string appId)
        {
            nativeWindow = new Platform.DesktopWindow();
            api = nativeWindow.CreateWindow(this, fullScreen, width, height, appId) as SkiaDrawingApi;
        }
    }
