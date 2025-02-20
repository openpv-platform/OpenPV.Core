using Ahsoka.Core.Drawing.Base;
using System.Runtime.InteropServices;

namespace Ahsoka.Core.Drawing.Platform;

internal class STWindow : LinuxWindow
{
    public STWindow() : base(new STDrawingImplementation())
    {

    }

    class STDrawingImplementation : LinuxDrawingImplementation
    {
        #region Native Interop
        public const string NativeLibraryName = "AhsokaLib/Ahsoka.Drawing.OpenViewLinux.so";

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int InitWindowWithSize(bool fullscreen, int width, int height, [MarshalAs(UnmanagedType.LPStr)] string appid);
        internal override int InitNativeWindowWithSize(bool fullscreen, int width, int height, string appId)
        {
            if (String.IsNullOrEmpty(appId))
                appId = "LinuxDrawingWindow";

            return InitWindowWithSize(fullscreen, width, height, appId);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int InitWindowWithSurfaceId(int surfaceId, [MarshalAs(UnmanagedType.LPStr)] string appid);
        internal override int InitNativeWindowWithSurfaceId(int surfaceId, string appId)
        {
            if (String.IsNullOrEmpty(appId))
                appId = "LinuxDrawingWindow";

            return InitWindowWithSurfaceId(surfaceId, appId);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetWindowDimensions(ref int w, ref int h);
        internal override void GetWindowSize(ref int w, ref int h)
        {
            GetWindowDimensions(ref w, ref h);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetSurfaceInfo(ref int samples, ref int stencilBits, ref uint gl_format);
        internal override void GetSurfaceInformation(ref int sampleCount, ref int stencilBits, ref uint gl_format)
        {
            GetSurfaceInfo(ref sampleCount, ref stencilBits, ref gl_format);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetFunction(string functionName);
        internal override nint GetFunctionAddress(string functionName)
        {
            return GetFunction(functionName);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr UpdateSwapInterval(int interval);
        internal override void SetSwapInterval(int interval)
        {
            UpdateSwapInterval(interval);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GLSwapBuffers();
        internal override void SwapBuffers()
        {
            GLSwapBuffers();
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetTouchData(ref TouchData data, int timeoutInMs = 1000);
        internal override bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000)
        {
            return GetTouchData(ref data, timeoutInMs);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DestoryWindowContext();
        internal override void DestroyContext()
        {
            DestoryWindowContext();
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdateSurfaceLocation(int surfaceId, int x, int y, int width, int height, bool commitChanges);
        internal override int UpdateLocationRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges = true)
        {
            return UpdateSurfaceLocation(surfaceId, x, y, w, h, commitChanges);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdateSurfaceSource(int surfaceId, int x, int y, int width, int height, bool commitChanges);
        internal override int UpdateSourceRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges = true)
        {
            return UpdateSurfaceSource(surfaceId, x, y, w, h, commitChanges);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdateSurfaceVisibility(int surfaceId, bool visibility, float opacity, bool commitChanges);
        internal override int UpdateVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges = true)
        {
            return UpdateSurfaceVisibility(surfaceId, isVisible, visibility, commitChanges);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetSurfaceVisibility(int surfaceId);
        internal override bool GetVisibility(int surfaceId)
        {
            return GetSurfaceVisibility(surfaceId);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern float GetSurfaceOpacity(int surfaceId);
        internal override float GetOpacity(int surfaceId)
        {
            return GetSurfaceOpacity(surfaceId);
        }

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetSurfaceLocation(int surfaceId, ref int x, ref int y, ref int w, ref int h);
        internal override DrawingRect GetLocationRectangle(int surfaceId)
        {
            int x, y, w, h;
            x = y = h = w = 0;
            GetSurfaceLocation(surfaceId, ref x, ref y, ref w, ref h);

            return new DrawingRect(x, y, w, h);
        }
        #endregion
    }

}

