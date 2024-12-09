using System;
using System.Collections.Generic;

namespace Ahsoka.Services.Device;

/// <summary>
/// Device Info
/// </summary>
public partial class DeviceInfo
{ 
   
    /// <summary>
    /// Get List of Services supporting this Type
    /// </summary>
    /// <returns></returns>
    public static string[] GetSupportedServices(DeviceTypes deviceTypes)
    {
        switch (deviceTypes)
        {
            case DeviceTypes.OnboardWirelessComboModule:
                return "WiFiClientService, WiFiAPService, BluetoothService, BluetoothDataService, BLEService".Split(',', StringSplitOptions.RemoveEmptyEntries);
            case DeviceTypes.OnboardWifiModule:
                return "WiFiClientService, WiFiAccessPointService".Split(',', StringSplitOptions.RemoveEmptyEntries);
            case DeviceTypes.OnboardBtModule:
                return "BluetoothService, BluetoothDataService, BLEService".Split(',', StringSplitOptions.RemoveEmptyEntries);
            case DeviceTypes.OnboardEthernet:
                return "NetworkService".Split(',', StringSplitOptions.RemoveEmptyEntries);
            case DeviceTypes.UsbCellularNetwork:
                return "CellularNetworkService".Split(',', StringSplitOptions.RemoveEmptyEntries);
            case DeviceTypes.IoControlModule:
                return "CANService, IOService".Split(',', StringSplitOptions.RemoveEmptyEntries);
            case DeviceTypes.DeviceTypeUnknown:
            default:
                return "NoService".Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
    }

}