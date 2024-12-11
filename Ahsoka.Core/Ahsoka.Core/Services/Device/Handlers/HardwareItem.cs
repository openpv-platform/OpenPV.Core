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
    public static SupportedFeatures[] GetSupportedServices(DeviceTypes deviceTypes)
    {
        switch (deviceTypes)
        {
            case DeviceTypes.OnboardWirelessComboModule:
                return [SupportedFeatures.Wifi, SupportedFeatures.BluetoothLe, SupportedFeatures.BluetoothClassic];
            case DeviceTypes.OnboardWifiModule:
                return [SupportedFeatures.Wifi];
            case DeviceTypes.OnboardBtModule:
                return [SupportedFeatures.BluetoothLe, SupportedFeatures.BluetoothClassic];
            case DeviceTypes.OnboardEthernet:
                return [SupportedFeatures.Ethernet];
            case DeviceTypes.UsbCellularNetwork:
                return [SupportedFeatures.Cellular];
            case DeviceTypes.IoControlModule:
                return [SupportedFeatures.Can, SupportedFeatures.Io];
            case DeviceTypes.DeviceTypeUnknown:
            default:
                return [SupportedFeatures.FeaturesUnknown];
        }
    }

}