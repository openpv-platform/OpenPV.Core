using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Ahsoka.Utility;

namespace Ahsoka.Storyboard;

[ExcludeFromCodeCoverage]
internal class MacOSStoryboardApi : IStoryboardIO
{
    const string NativeLibraryName = $"AhsokaLib/Ahsoka.Storyboard.macOSArm64.so";

    //IntPtr used as cover for gre_io_t * opaque type
    //IntPtr also used as cover for gre_io_serialize_t * non-opaque type

    [DllImport(NativeLibraryName)]
    public static extern IntPtr gre_io_open(string name, int flag);

    [DllImport(NativeLibraryName)]
    public static extern void gre_io_close(IntPtr handle);

    [DllImport(NativeLibraryName)]
    public static extern int gre_io_send(IntPtr handle, IntPtr buffer);

    [DllImport(NativeLibraryName)]
    unsafe public static extern int gre_io_receive(IntPtr handle, IntPtr* buffer);

    // Data Serialization Functions
    [DllImport(NativeLibraryName)]
    public static extern IntPtr gre_io_serialize(IntPtr buffer, string event_addr, string event_name, string event_format, byte[] event_data, int event_nbytes);

    [DllImport(NativeLibraryName)]
    public static extern int gre_io_unserialize(IntPtr buffer, ref IntPtr event_addr, ref IntPtr event_name, ref IntPtr event_format, ref IntPtr event_data);

    [DllImport(NativeLibraryName)]
    public static extern void gre_io_free_buffer(IntPtr buffer);
}

[ExcludeFromCodeCoverage]
internal class MacOStoryboardInterop : IStoryboardAPI<MacOSStoryboardApi>
{
    public List<ChannelData> Channels { get; set; } = new();

    public string EngineRootPath()
    {
        return $"{AppContext.BaseDirectory}Storyboard/MacosArm64/";
    }

    public string GetSBEnginePath()
    {
        return $"{EngineRootPath()}sbengine/bin/sbengine.app/Contents/MacOS/sbengine_cocoa";
    }

    public Process StartProcess(string sbEnginePath, string pathToSBProject, string startupArguments)
    {
        // Construct the SB startup command and run
        Process.Start("chmod", $"+x {sbEnginePath}").WaitForExit();
        return Process.Start(sbEnginePath, pathToSBProject);
    }
}