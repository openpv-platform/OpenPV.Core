using System.Runtime.InteropServices;

namespace Ahsoka.Core.Drawing.Cairo;

internal class STCairoWindow : LinuxCairoWindow
{
    public STCairoWindow():base(new STCairoDrawingImplementation())
    {

    }

    class STCairoDrawingImplementation : CairoDrawingImplementation
    {
        #region Native Interop
        public const string NativeLibraryName = "AhsokaLib/Ahsoka.Cairo.OpenViewLinux.so";

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

        internal override int InitWindow(int x, int y, int w, int h, bool fullscreen)
        {
            return InitWindowInterop(x, y, w, h, fullscreen);
        }

        internal override nint GetSurface()
        {
            return GetCairoSurface();
        }

        internal override void SwapBuffers(nint p)
        {
            GLSwapBuffers(p);
        }

        internal override bool UpdateTouchData(ref TouchData data, int timeoutInMs = 1000)
        {
            return GetTouchData(ref data, timeoutInMs);
        }

        internal override void UpdateWindowDimensions(ref int w, ref int h)
        {
            GetWindowDimensions(ref w, ref h);
        }

        internal override void DestroyContext()
        {
            DestoryWindowContext();
        }

        #endregion
    }

}

