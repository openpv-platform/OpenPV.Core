using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ahsoka.Storyboard;

internal struct ChannelData
{
    public string name;
    public IntPtr handle; //gre_io_t* handle;
    public int type;
    public bool destroyed;
}

internal interface IStoryboardIO
{
    #pragma warning disable IDE1006 // Naming Styles
    static abstract IntPtr gre_io_open(string name, int flag);
    static abstract void gre_io_close(IntPtr handle);
    static abstract int gre_io_send(IntPtr handle, IntPtr buffer);
    unsafe static abstract int gre_io_receive(IntPtr handle, IntPtr* buffer);

    // Data Serialization Functions
    static abstract IntPtr gre_io_serialize(IntPtr buffer, string event_addr, string event_name, string event_format, byte[] event_data, int event_nbytes);
    static abstract int gre_io_unserialize(IntPtr buffer, ref IntPtr event_addr, ref IntPtr event_name, ref IntPtr event_format, ref IntPtr event_data);
    static abstract void gre_io_free_buffer(IntPtr buffer);
}

internal interface IStoryboardAPI<StoryboardIO> : IStoryboardAPI where StoryboardIO : IStoryboardIO
{
    public static readonly int GRE_IO_TYPE_RDONLY = 0x0;   // Open channel for read access
    public static readonly int GRE_IO_TYPE_XRDONLY = 0x1;   // Open channel for exclusive read access
    public static readonly int GRE_IO_TYPE_WRONLY = 0x2;   // Open channel for write access
    public static readonly int GRE_IO_TYPE_MASK = 0xF;   // Type mask

    public static readonly int GRE_IO_FLAG_NONBLOCK = 0x0010;   // Open channel nonblocking
    public static readonly int GRE_IO_FLAG_MASK = 0xFFF0;   // Special flag mask
    public static readonly int GRE_IO_MAX_NAME_LEN = 25;       // Maximum name gre_io_open name length
    public static readonly int GRE_IO_MAX_MSG_UNLIMITED = 0;

    List<ChannelData> Channels { get; set; }

    [ExcludeFromCodeCoverage]
    int CreateChannel(string channelName, int flag)
    {
        IntPtr handle = StoryboardIO.gre_io_open(channelName, flag);

        if (handle != 0)
        {
            ChannelData channel = new() { name = channelName, handle = handle, type = flag, destroyed = false };
            Channels.Add(channel);
            return Channels.Count - 1;
        }
        else
        {
            return -1;
        }
    }

    [ExcludeFromCodeCoverage]
    bool ValidateChannel(int id)
    {
        if (id < 0 || id >= Channels.Count)
            return false;
        if (Channels[id].type != GRE_IO_TYPE_WRONLY)
            return false;
        if (Channels[id].destroyed == true)
            return false;

        return true;
    }

    [ExcludeFromCodeCoverage]
    void IStoryboardAPI.DestroyChannel(int channel)
    {
        int result;
        if (channel < 0 || channel >= Channels.Count)
        {
            result = -1;
        }
        else
        {
            ChannelData data = Channels[channel];
            StoryboardIO.gre_io_close(data.handle);
            data.name = "";
            data.type = GRE_IO_TYPE_MASK;
            data.destroyed = true;
            Channels[channel] = data;
            result = 0;
        }

        if (result != 0)
            Console.Write($"Destroy Channel {channel} with result {result}");
    }

    [ExcludeFromCodeCoverage]
    int IStoryboardAPI.CreateInboundChannel(string channelName)
    {
        return CreateChannel(channelName, GRE_IO_TYPE_RDONLY);
    }

    [ExcludeFromCodeCoverage]
    int IStoryboardAPI.CreateOutboundChannel(string channelName)
    {
        return CreateChannel(channelName, GRE_IO_TYPE_WRONLY);
    }

    [ExcludeFromCodeCoverage]
    StoryboardMessage IStoryboardAPI.ReceiveEvent(int id, int timeoutInMs)
    {
        IntPtr buffer = IntPtr.Zero;
        IntPtr targetPtr = IntPtr.Zero;
        IntPtr namePtr = IntPtr.Zero;
        IntPtr formatPtr = IntPtr.Zero;
        IntPtr dataPtr = IntPtr.Zero;
        byte[] data = Array.Empty<byte>();
        StoryboardMessage message;

        unsafe
        {
            int sizeOfMessage = StoryboardIO.gre_io_receive(Channels[id].handle, &buffer);
            if (sizeOfMessage < 0)
            {
                Console.WriteLine("Failed to receive event");
            }
            else
            {
                int numBytes = StoryboardIO.gre_io_unserialize(buffer, ref targetPtr, ref namePtr, ref formatPtr, ref dataPtr);
                if (numBytes < 0)
                {
                    Console.WriteLine("Failed to unserialize data");
                    StoryboardIO.gre_io_free_buffer(buffer);
                }
                else
                {
                    Array.Resize(ref data, numBytes);
                    Marshal.Copy(dataPtr, data, 0, numBytes);
                }
            }

            message = new StoryboardMessage(Marshal.PtrToStringAnsi(namePtr),
                                            Marshal.PtrToStringAnsi(formatPtr),
                                            StoryboardMessageDirection.In.GetMessageDirectionString(),
                                            data,
                                            Marshal.PtrToStringAnsi(targetPtr));

            StoryboardIO.gre_io_free_buffer(buffer);
        }

        return message;
    }

    [ExcludeFromCodeCoverage]
    int IStoryboardAPI.SendEvent(int id, StoryboardMessage message)
    {
        int status;

        if (!ValidateChannel(id))
            return -1;

        IntPtr channelBuffer = IntPtr.Zero;
        unsafe
        {
            channelBuffer = StoryboardIO.gre_io_serialize(channelBuffer, message.Target, message.EventAttribute.EventName, message.Format, message.Data, message.Data.Length);
        }

        if (channelBuffer == IntPtr.Zero)
        {
            Console.WriteLine("Can't serialize data to buffer");
            return -1;
        }
        else
        {
            unsafe
            {
                status = StoryboardIO.gre_io_send(Channels[id].handle, channelBuffer);
            }

            if (status < 0)
            {
                Console.WriteLine("Can't send event");
            }
        }

        StoryboardIO.gre_io_free_buffer(channelBuffer);

        return status;
    }
}