using System.Runtime.InteropServices;

namespace Ahsoka.Core.Drawing.Platform;

internal static class LinuxFontConfig
{
    const string lib = "libfontconfig.so.1";

    [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
    static extern int FcConfigAppFontAddFile(IntPtr config, string fileName);

    [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr FcConfigCreate();

    [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr FcConfigDestroy(IntPtr config);

    [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr FcConfigGetCurrent();

    [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
    static extern int FcConfigSetCurrent(IntPtr config);

    [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr FcConfigGetFonts(IntPtr config, int setName);

    static readonly IntPtr appConfig;
    static LinuxFontConfig()
    {
        // Construct Local Font Config and Make Current
        appConfig = FcConfigCreate();
        int result = FcConfigSetCurrent(appConfig);
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Font Config Initialized with Result:{result}");
    }

    public static bool AddFont(string fileName)
    {
        int result = FcConfigAppFontAddFile(appConfig, fileName);
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Font Added to Collection With Result:{result}");

        return result == 1;
    }

    public static IntPtr GetFontSet()
    {
        var returnValue = FcConfigGetFonts(appConfig, 1);// return Fonts

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Getting Font Set:{returnValue}");
        return returnValue;

    }
}