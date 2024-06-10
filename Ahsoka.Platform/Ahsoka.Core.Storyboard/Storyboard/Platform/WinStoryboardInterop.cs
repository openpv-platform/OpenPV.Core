using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Ahsoka.Storyboard;

internal class WinStoryboardApi : IStoryboardIO
{
    const string NativeLibraryName = "AhsokaLib/Ahsoka.Storyboard.Windows.dll";

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

internal class WinStoryboardInterop : IStoryboardAPI<WinStoryboardApi>
{
    public List<ChannelData> Channels { get; set; } = new();

    public string EngineRootPath()
    {
        return $".\\Storyboard\\Windows64\\";
    }

    public string GetSBEnginePath()
    {
        return Path.Combine(EngineRootPath(), "sbengine\\bin\\sbengine.exe");
    }

    public Process StartProcess(string enginePath, string pathToSBProject, string startupArguments)
    {
        string startupCommand = $"{enginePath} {startupArguments} {pathToSBProject}";
        return Process.Start("cmd" , $"/c \"{startupCommand}\"");
    }
}
