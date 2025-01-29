using Ahsoka.ServiceFramework;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Ahsoka.Services.Device;

internal static class USBRoleSwap
{
    #region NativeLibrary
    internal const string usbRoleDriverPath = "/sys/devices/platform/soc/49000000.usb-otg/usb_role/49000000.usb-otg-role-switch/role";
    #endregion

    const int O_WRONLY = 1;

    const int GPIO_INDEX = 5;
    const string GPIO_DEV = "/dev/gpiochip";

    const int CC_RD_EN_PIN = 7;
    const int CC_RP_EN_PIN = 9;

    const string FILE_DESC_HOST_ROLE = "host";
    const int FILE_DESC_HOST_ROLE_COUNT = 4;

    const string FILE_DESC_PERIPHERAL_ROLE = "none";
    const int FILE_DESC_PERIPHERAL_ROLE_COUNT = 4;

    const uint GPIO_GET_LINEHANDLE_IOCTL = 3245126659; // 0xC16CB403 _IOWR(0xB4, 0x03, struct gpiohandle_request)
    const uint GPIOHANDLE_SET_LINE_VALUES_IOCTL = 3225465865; // C040B409 _IOWR(0xB4, 0x09, struct gpiohandle_data)

    internal unsafe static int ChangeModes(UsbPort port, UsbMode device_role)
    {
        if (port == UsbPort.Secondary)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Secondary Port Not Supported on this Platform");
            return -1;
        }

        int ret = 0;

        string usb_role_write = null;
        int usb_role_write_count = 0;
        byte gpio_set_value;

        switch (device_role)
        {
            case UsbMode.Host:
                usb_role_write = FILE_DESC_HOST_ROLE;
                usb_role_write_count = FILE_DESC_HOST_ROLE_COUNT;
                gpio_set_value = 0;
                break;
            case UsbMode.Device:
                usb_role_write = FILE_DESC_PERIPHERAL_ROLE;
                usb_role_write_count = FILE_DESC_PERIPHERAL_ROLE_COUNT;
                gpio_set_value = 1;
                break;
            default:
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "An unrecognized usb role was requested");
                return -1;
        }


        //////////////////////////////////////////////////
        // GPIO setup
        //////////////////////////////////////////////////
        GpioHandleRequest req_rd, req_rp;
        GpioHandleData data_rd, data_rp;
        SafeFileHandle file_desc_gpio;

        // Open the file descript for the gpiochip that contains the two pins that we are going to be modifying
        ret = OpenDevice(out file_desc_gpio, GPIO_INDEX);
        if (ret != 0) return ret;

        // open up the linehandles for the two pins that we are going to want to modify
        ret = open_pin_linehandle(out req_rd, out data_rd, ref file_desc_gpio, CC_RD_EN_PIN);
        if (ret != 0) return ret;
        ret = open_pin_linehandle(out req_rp, out data_rp, ref file_desc_gpio, CC_RP_EN_PIN);
        if (ret != 0) return ret;

        //////////////////////////////////////////////////
        // usb role setup
        //////////////////////////////////////////////////
        // Open the file descriptor for the usb role to be able to change it
        var file_desc_usb_role = Open(usbRoleDriverPath, O_WRONLY);

        //////////////////////////////////////////////////
        // Logic
        //////////////////////////////////////////////////
        int write_count = Write(file_desc_usb_role, usb_role_write, usb_role_write_count);
        if (write_count != usb_role_write_count)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "The usb role file descriptor was unsuccessfully written to.");
            return -1;
        }

        ret = set_pin_value(ref req_rd, ref data_rd, gpio_set_value);
        if (ret != 0) return ret;
        ret = set_pin_value(ref req_rp, ref data_rp, gpio_set_value);
        if (ret != 0) return ret;

        //////////////////////////////////////////////////
        // cleanup
        //////////////////////////////////////////////////
        // close the linehandles for the two pins that we were modifying
        ret = close_pin_linehandle(ref req_rd);
        if (ret != 0) return ret;

        ret = close_pin_linehandle(ref req_rp);
        if (ret != 0) return ret;

        return ret;

    }

    private unsafe static int OpenDevice(out SafeFileHandle file, int gpiochip_index)
    {
        string gpio_dev_name = $"{GPIO_DEV}{gpiochip_index}";
        int ret = 0;

        // Open the file descriptor for the device
        file = Open(gpio_dev_name, 0);
        if (file.IsInvalid)
        {
            // If the file descriptor wasn't successfully able to be opened, return an error
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Failed to open the file descriptor: {gpio_dev_name}");
            return -1;
        }

        return ret;
    }


    private unsafe static int open_pin_linehandle(out GpioHandleRequest req, out GpioHandleData data, ref SafeFileHandle file, uint pin_index)
    {
        int ret = 0;

        req = new GpioHandleRequest();
        data = new GpioHandleData();

        // Request the GPIO linehandle for the given pin index
        req.lineoffsets[0] = pin_index;
        req.flags = ((uint)1 << 1); //GPIOHANDLE_REQUEST_OUTPUT	(1UL << 1)

        Array.Copy(req.default_values, data.values, 64);

        // strcpy(req.consumer_label, "led_gpio_a_14");
        req.lines = 1;

        ret = Ioctl(file.DangerousGetHandle(), GPIO_GET_LINEHANDLE_IOCTL, ref req);
        if (ret == -1)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Failed to issue GET LINEHANDLE IOCTL ({ret})");
            return -1;
        }

        return ret;
    }

    private unsafe static int set_pin_value(ref GpioHandleRequest request, ref GpioHandleData data, byte gpio_set_value)
    {
        data.values[0] = gpio_set_value;
        int ret = Ioctl(request.fd, GPIOHANDLE_SET_LINE_VALUES_IOCTL, ref data);
        if (ret == -1)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Failed to issue GPIOHANDLE_SET_LINE_VALUES_IOCTL ({ret})");
            return -1;
        }

        return ret;
    }


    private unsafe static int close_pin_linehandle(ref GpioHandleRequest request)
    {
        int ret = Close(request.fd);
        if (ret == -1)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Failed to close GPIO LINEHANDLE device file");
            return -1;
        }

        return ret;
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern SafeFileHandle Open(string pathname, int flags);

    [DllImport("libc", EntryPoint = "write", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int Write(SafeFileHandle handle, string pathname, int flags);

    [DllImport("libc", EntryPoint = "close", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int Close(nint handle);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    internal static extern int Ioctl(nint serialHandle, uint request, ref GpioHandleData data);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    internal static extern int Ioctl(nint serialHandle, uint request, ref GpioHandleRequest data);
}


[StructLayout(LayoutKind.Sequential)]
struct GpioHandleData
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] values;

    public GpioHandleData()
    {
        values = new byte[64];
    }
};

[StructLayout(LayoutKind.Sequential)]
struct GpioHandleRequest
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public uint[] lineoffsets;

    public uint flags;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] default_values;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public char[] consumer_label;

    public uint lines;

    public int fd;

    public GpioHandleRequest()
    {
        lineoffsets = new uint[64];
        default_values = new byte[64];
        consumer_label = new char[32];
    }
};
