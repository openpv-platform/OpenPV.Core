using Ahsoka.Services.Bluetooth.LE.Bluez.Core;
using Ahsoka.Services.Bluez.Core;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

internal class GattService : PropertiesBase<GattService1Properties>, IGattService1, IObjectManagerProperties
{
    private readonly IList<GattCharacteristic> _Characteristics = new List<GattCharacteristic>();

    public IEnumerable<GattCharacteristic> Characteristics => _Characteristics;
    public string UUID => Properties.UUID;

    public GattService(string objectPath, GattService1Properties properties) : base(objectPath, properties)
    {
    }

    public IDictionary<string, IDictionary<string, object>> GetProperties()
    {
        return new Dictionary<string, IDictionary<string, object>>
        {
            {
                "org.bluez.GattService1", new Dictionary<string, object>
                {
                    {"UUID", Properties.UUID},
                    {"Primary", Properties.Primary},
                    {"Characteristics", Characteristics.Select(c => c.ObjectPath).ToArray()}
                }
            }
        };
    }

    public GattCharacteristic AddCharacteristic(GattCharacteristic1Properties characteristic, BLECharacteristicProperties properties, LinuxImplementation service)
    {
        characteristic.Service = ObjectPath;
        var gattCharacteristic = new GattCharacteristic(NextCharacteristicPath(), properties, characteristic, service);
        _Characteristics.Add(gattCharacteristic);

        Properties.Characteristics = Properties.Characteristics.Append(NextCharacteristicPath()).ToArray();

        return gattCharacteristic;
    }

    private string NextCharacteristicPath()
    {
        return ObjectPath + "/characteristic" + _Characteristics.Count;
    }
}