using Ahsoka.Utility;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal static class SerialRS485Constants
{
    // If enabled
    public const UInt32 SER_RS485_ENABLED = (1 << 0);
    // Logical level for RTS pin when sending
    public const UInt32 SER_RS485_RTS_ON_SEND = (1 << 1);
    // Logical level for RTS pin after sent
    public const UInt32 SER_RS485_RTS_AFTER_SEND = (1 << 2);
    public const UInt32 SER_RS485_RX_DURING_TX = (1 << 4);
    // Enable bus termination (if supported)
    public const UInt32 SER_RS485_TERMINATE_BUS = (1 << 5);

    // This is the request value that is going to be passed through to the ioctl function to request a change to the
    // re485 configuration with the referenced rs485 configuration structure.
    public const int TIOCSRS485 = 21551; // 0x542F

    // These are going to be the flags that are passed through to the open function when opening up the serial serial
    //  port on the unit.
    public const int O_RDWR = 2; // 0x00000002
    public const int O_NONBLOCK = 2048; // 0x00000800
}

/// <summary>
/// Structure for Configuring RS485 on Linux
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SerialRS485
{
    /// <summary>
    /// Gets the RS485 Feature Flags
    /// </summary>
    public UInt32 flags;

    /// <summary>
    /// Delay before send (milliseconds)
    /// </summary>
    public UInt32 delay_rts_before_send;

    /// <summary>
    /// Delay after send (milliseconds)
    /// </summary>
    public UInt32 delay_rts_after_send;

    /// <summary>
    /// Padding
    /// </summary>
    // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public UInt32[] padding; // Memory padding

    /// <summary>
    /// Provide a simple function to set flag values ono the struct.
    /// </summary>
    /// <param name="serial_rs485_config">Configuration to Enable</param>
    /// <param name="flag">Flag to Enable/Disable</param>
    /// <param name="enable">Enable or Disable</param>
    [ExcludeFromCodeCoverage]
    public static void SetRS485FieldBit(ref SerialRS485 serial_rs485_config, UInt32 flag, bool enable)
    {
        if (enable)
        {
            serial_rs485_config.flags |= flag;
        }
        else
        {
            serial_rs485_config.flags &= ~flag;
        }
    }

    /// <summary>
    /// Get the Default Configuration.
    /// </summary>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    public static SerialRS485 GetDefaultRS485Config()
    {
        // NOTE: This is the default configuration for the RS485 ports on boot
        return new SerialRS485()
        {
            flags = (SerialRS485Constants.SER_RS485_ENABLED | SerialRS485Constants.SER_RS485_RTS_ON_SEND) &
                    ~(SerialRS485Constants.SER_RS485_RTS_AFTER_SEND),
            delay_rts_after_send = 60,
            delay_rts_before_send = 60
        };
    }
}

[ExcludeFromCodeCoverage]
internal class LinuxSystemUtilities
{
    #region libc
    public static int Errno { get { return Marshal.GetLastWin32Error(); } }

    [DllImport("libc", EntryPoint = "open", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern SafeFileHandle Open(string pathname, int flags);

    /// <summary>
    /// This is the function template to set the configuration of the RS485 serial outputs in a linux system.
    /// </summary>
    /// <param name="serialHandle">This is the socket handle for the socket that is being worked with.</param>
    /// <param name="request">The request that is going to be used. In the case of this function we always expect this
    /// value to be the TIOCSRS485 constant.</param>
    /// <param name="rs485_conf">The rs485 configuration structure that is going to configure the output.</param>
    /// <returns></returns>
    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    internal static extern int Ioctl(SafeFileHandle serialHandle, int request, ref SerialRS485 rs485_conf);
    #endregion

    internal static void Reboot()
    {
        Console.WriteLine($"/sbin/reboot");
        ProcessUtility.RunProcess("/sbin/reboot", null, null, out string log, out string error);
    }
}