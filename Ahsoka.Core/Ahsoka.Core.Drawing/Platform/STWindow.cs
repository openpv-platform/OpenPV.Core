using Ahsoka.Core.Drawing.Platform;
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

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int InitWindowInterop(int x, int y, int w, int h, bool fullscreen);
      
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

        internal override int InitWindow(int x, int y, int w, int h, bool fullscreen)
        {
            return InitWindowInterop(x, y, w, h, fullscreen);
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

        #endregion
    }

}

