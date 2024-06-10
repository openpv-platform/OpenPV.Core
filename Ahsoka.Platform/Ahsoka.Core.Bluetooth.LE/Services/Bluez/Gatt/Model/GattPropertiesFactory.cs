using System;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

internal class GattPropertiesFactory
{
    public static GattService1Properties CreateGattService(BLEServiceProperties serviceDescription)
    {
        return new GattService1Properties
        {
            UUID = serviceDescription.Uuid,
            Primary = serviceDescription.Primary,
            Characteristics = Array.Empty<ObjectPath>()
        };
    }

    public static GattCharacteristic1Properties CreateGattCharacteristic(BLECharacteristicProperties characteristic)
    {
        var BLECharacteristicProperties = new GattCharacteristic1Properties
        {
            UUID = characteristic.Uuid,
            Flags = FlagConverter.ConvertCharacteristicFlags(characteristic.Flags)
        };

        return BLECharacteristicProperties;
    }

    public static GattDescriptor1Properties CreateGattDescriptor(BLECharacteristicDescriptorProperties charDescriptorProperties)
    {
        var descriptorProperties = new GattDescriptor1Properties
        {
            UUID = charDescriptorProperties.Uuid,
            Flags = FlagConverter.ConvertDescriptorFlags(charDescriptorProperties.Flags),
            Value = charDescriptorProperties.Value
        };

        return descriptorProperties;
    }
}