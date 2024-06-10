using Ahsoka.Services.Bluetooth.LE.Bluez.Core;
using Ahsoka.Services.Bluez.Core;
using Ahsoka.Services.Bluez.Gatt.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

internal class GattDescriptor : PropertiesBase<GattDescriptor1Properties>, IGattDescriptor1, IObjectManagerProperties
{
    public GattDescriptor(ObjectPath objectPath, GattDescriptor1Properties gattDescriptor1Properties)
        : base(objectPath, gattDescriptor1Properties)
    {

    }

    public Task<byte[]> ReadValueAsync(IDictionary<string, object> Options)
    {
        ushort offsetValue = 0;
        if (Options.ContainsKey(ReadWriteUtility.ReadOffsetKeyString))
            offsetValue = (ushort)Options[ReadWriteUtility.ReadOffsetKeyString];

        return Task.FromResult(ReadWriteUtility.GetValueAtOffset(Properties.Value, offsetValue));
    }

    public IDictionary<string, IDictionary<string, object>> GetProperties()
    {
        return new Dictionary<string, IDictionary<string, object>>()
        {
            {
                "org.bluez.GattDescriptor1", new Dictionary<string, object>
                {
                    { "Characteristic", Properties.Characteristic },
                    { "UUID", Properties.UUID },
                    { "Flags", Properties.Flags }
                }
            }
        };
    }
}