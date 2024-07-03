using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ahsoka.Services.Network.ESPWifi;

[ExcludeFromCodeCoverage]
internal class ESPInterop
{
    #region Methods
    public ESPInterop()
    {
    }

    public WifiMode WifiGetMode()
    {
        wifi_get_mode(out int mode);
        return (WifiMode)mode;
    }

    public CommandResult WifiSetMode(WifiMode mode)
    {
        return (CommandResult)wifi_set_mode((int)mode);
    }

    public string WifiGetMAC(WifiMode mode)
    {
        MacString mac = new();

        wifi_get_mac((int)mode, ref mac);

        if (mac.mac is null)
            return new string("");
        else
            return mac.mac;
    }

    public void WifiSetMAC(WifiMode mode, string mac)
    {
        wifi_set_mac((int)mode, mac);
    }

    public void WifiSetTxPower(WifiTxPowerLevel power)
    {
        wifi_set_max_tx_power((sbyte)power);
    }

    public WifiTxPowerLevel WifiGetTxPower()
    {
        wifi_get_curr_tx_power(out sbyte power);
        return (WifiTxPowerLevel)power;
    }

    public CommandResult WifiSetPowerSaveMode(WifiPowerSaveMode mode)
    {
        return (CommandResult)wifi_set_power_save_mode((int)mode);
    }

    public WifiPowerSaveMode WifiGetPowerSaveMode()
    {
        wifi_get_power_save_mode(out int mode);
        return (WifiPowerSaveMode)mode;
    }

    public ConnectResult WifiConnect(ESPHostedStationConfig apConfig)
    {
        return (ConnectResult)wifi_set_ap_config(apConfig);
    }

    public CommandResult WifiGetConnectionStatus(ref ESPHostedStationConfig apConfig)
    {
        return (CommandResult)wifi_get_ap_config(ref apConfig);
    }

    public List<ESPHostedWifiScanList> WifiStartScanning()
    {
        List<ESPHostedWifiScanList> apList = new();
        IntPtr pScanList = wifi_ap_scan_list(out int count);
        IntPtr pScanItem = pScanList;

        for (int i = 0; i < count; i++)
        {
            ESPHostedWifiScanList ap = Marshal.PtrToStructure<ESPHostedWifiScanList>(pScanItem);
            apList.Add(ap);
            pScanItem = IntPtr.Add(pScanItem, Marshal.SizeOf(ap));
        }
        esp_hosted_free(pScanList);
        return apList;
    }

    public CommandResult WifiDisconnect()
    {
        return (CommandResult)wifi_disconnect_ap();
    }

    public void WifiSetAPConfiguration(ESPHostedSoftAPConfig apConfig)
    {
        wifi_set_softap_config(apConfig);
    }

    public void WifiGetAPConfiguration(ref ESPHostedSoftAPConfig apConfig)
    {
        wifi_get_softap_config(ref apConfig);
    }

    public List<ESPHostedWifiConnectedStationsList> WifiConnectedStations()
    {
        List<ESPHostedWifiConnectedStationsList> stationList = new();
        IntPtr pScanList = wifi_connected_stations_list(out int count);
        IntPtr pScanItem = pScanList;

        for (int i = 0; i < count; i++)
        {
            ESPHostedWifiConnectedStationsList st = Marshal.PtrToStructure<ESPHostedWifiConnectedStationsList>(pScanItem);
            stationList.Add(st);
            pScanItem = IntPtr.Add(pScanItem, Marshal.SizeOf(st));
        }
        esp_hosted_free(pScanList);
        return stationList;
    }
    #endregion

    #region Extern


    // TODO: Currently not implemented from commands.h
    //   AP Specific Functionality:
    //     int wifi_set_vendor_specific_ie(bool enable, wifi_vendor_ie_type_t type, wifi_vendor_ie_id_t idx, void* vnd_ie, uint16_t vnd_ie_size);
    //   OTA Specific Functionality:
    //   (we have the functionality to update the ESP32 module firmware with a BSP update)
    //     int esp_ota_begin();
    //     int esp_ota_write(uint8_t* ota_data, uint32_t ota_data_len);
    //     int esp_ota_end();
    const string NativeLibraryName = "AhsokaLib/Ahsoka.Wifi.ST.so";

    [DllImport(NativeLibraryName)]
    static extern int wifi_get_mode(out int mode);

    [DllImport(NativeLibraryName)]
    static extern int wifi_set_mode(int mode);

    [DllImport(NativeLibraryName, CharSet = CharSet.Ansi)]
    static extern int wifi_get_mac(int mode, ref MacString mac);

    [DllImport(NativeLibraryName, CharSet = CharSet.Ansi)]
    static extern int wifi_set_mac(int mode, [MarshalAs(UnmanagedType.LPStr)] string mac);

    [DllImport(NativeLibraryName)]
    static extern int wifi_get_power_save_mode(out int mode);

    [DllImport(NativeLibraryName)]
    static extern int wifi_set_power_save_mode(int mode);

    [DllImport(NativeLibraryName)]
    static extern int wifi_get_curr_tx_power(out sbyte tx_power);

    [DllImport(NativeLibraryName)]
    static extern int wifi_set_max_tx_power(sbyte tx_power);

    [DllImport(NativeLibraryName)]
    static extern int wifi_disconnect_ap();

    [DllImport(NativeLibraryName, EntryPoint = "wifi_set_ap_config")]
    static extern int wifi_set_ap_config(ESPHostedStationConfig ap_config);

    /* 
     * dll accepts a union of two structures. If we ever need the other
     * structure, it is easier to treat it by overloading the function as shown here:
     [DllImport(NativeLibraryName, EntryPoint = "wifi_set_ap_config")]
     static extern int wifi_set_ap_config(ESPHostedSoftAPConfig ap_config);
    */

    [DllImport(NativeLibraryName, EntryPoint = "wifi_get_ap_config")]
    static extern int wifi_get_ap_config(ref ESPHostedStationConfig ap_config);

    /* 
     * dll accepts a union of two structures. If we ever need the other
     * structure, it is easier to treat it by overloading the function as shown here:
     [DllImport(NativeLibraryName, EntryPoint = "wifi_get_ap_config")]
     static extern int wifi_get_ap_config(ref ESPHostedSoftAPConfig ap_config);
    */

    [DllImport(NativeLibraryName)]
    static extern void esp_hosted_free(IntPtr ptr);

    [DllImport(NativeLibraryName)]
    static extern IntPtr wifi_ap_scan_list(out int count);

    [DllImport(NativeLibraryName)]
    static extern int wifi_set_softap_config(ESPHostedSoftAPConfig softap_config);

    [DllImport(NativeLibraryName)]
    static extern int wifi_get_softap_config(ref ESPHostedSoftAPConfig softap_config);

    [DllImport(NativeLibraryName)]
    static extern IntPtr wifi_connected_stations_list(out int count);

    [DllImport(NativeLibraryName)]
    static extern int wifi_stop_softap();
    #endregion
}

internal enum CommandResult
{
    Success = 0,
    Failure = -1
}

internal enum ConnectResult
{
    Success = 0,
    Failure = -1,
    NotFound = 2,
    InvalidPassword = 3
}
