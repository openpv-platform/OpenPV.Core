using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Platform;
using Ahsoka.ServiceFramework;
using System.Runtime.InteropServices;

namespace Ahsoka.Core.Drawing.Platform;

internal class STWindow : LinuxWindow
{
    public STWindow(): base(new STDrawingImplementation())
    {

    }

    class STDrawingImplementation : LinuxDrawingImplementation
    {
        #region Native Interop
        public const string NativeLibraryName = "AhsokaLib/Ahsoka.Drawing.OpenViewLinux.so";

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int InitWindowWithSize(bool fullscreen, int width, int height, int surfaceId, string appid);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int InitWindowWithSurfaceId(int surfaceId, string appid);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdateSurfaceLocation(int surfaceId, int x, int y, int width, int height, bool commitChanges);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdateSurfaceSource(int surfaceId, int x, int y, int width, int height, bool commitChanges);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdateSurfaceVisibility(int surfaceId, bool visibility, float opacity, bool commitChanges);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetWindowDimensions(ref int w, ref int h);
      
        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetSurfaceInfo(ref int samples, ref int stencilBits, ref uint gl_format);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetFunction(string functionName);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr UpdateSwapInterval(int interval);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GLSwapBuffers();

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetTouchData(ref TouchData data, int timeoutInMs = 1000);
    
        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DestoryWindowContext();

        internal override int InitNativeWindowWithSize(bool fullscreen, int width, int height, string appId, int surfaceId)
        {
            if (String.IsNullOrEmpty(appId))
                appId = "LinuxDrawingWindow";
          
            return InitWindowWithSize(fullscreen, width, height, surfaceId, appId);
        }

        internal override int InitNativeWindowWithSurfaceId(int surfaceId, string appId)
        {
            if (String.IsNullOrEmpty(appId))
                appId = "LinuxDrawingWindow";
          
            return InitWindowWithSurfaceId(surfaceId, appId);
        }

        internal override void GetWindowInformation(ref int w, ref int h)
        {
            GetWindowDimensions(ref w, ref h);
        }

        internal override void GetSurfaceInformation(ref int sampleCount, ref int stencilBits, ref uint gl_format)
        {
            GetSurfaceInfo(ref sampleCount, ref stencilBits, ref gl_format);
        }
     
        internal override nint GetFunctionAddress(string functionName)
        {
            return GetFunction(functionName);
        }
        
        internal override void SwapBuffers()
        {
            GLSwapBuffers();
        }

        internal override bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000)
        {
            return GetTouchData(ref data, timeoutInMs);
        }
        
        internal override void DestroyContext()
        {
            DestoryWindowContext();
        }

        internal override void SetSwapInterval(int interval)
        {
            UpdateSwapInterval(interval);
        }

        internal override int ChangeLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges = true)
        {
            return UpdateSurfaceLocation(surfaceId, x, y, w, h, commitChanges);
        }

        internal override int ChangeSource(int surfaceId, int x, int y, int w, int h, bool commitChanges = true)
        {
            return UpdateSurfaceSource(surfaceId, x, y, w, h, commitChanges);
        }

        internal override int ChangeVisbility(int surfaceId, bool isVisible, float visibility, bool commitChanges = true)
        {
            return UpdateSurfaceVisibility(surfaceId, isVisible, visibility, commitChanges);
        }

        #endregion
    }

}

