using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

[DBusInterface("org.bluez.GattDescriptor1")]
interface IGattDescriptor1 : IDBusObject
{
    Task<byte[]> ReadValueAsync(IDictionary<string, object> Options);
    Task<object> GetAsync(string prop);
    Task<GattDescriptor1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}



[Dictionary]
internal class GattDescriptor1Properties
{
    private string _UUID = default;
    public string UUID
    {
        get
        {
            return _UUID;
        }

        set
        {
            _UUID = value;
        }
    }

    private ObjectPath _Characteristic = default;
    public ObjectPath Characteristic
    {
        get
        {
            return _Characteristic;
        }

        set
        {
            _Characteristic = value;
        }
    }

    private byte[] _Value = default;
    public byte[] Value
    {
        get
        {
            return _Value;
        }

        set
        {
            _Value = value;
        }
    }

    private string[] _Flags = default;
    public string[] Flags
    {
        get
        {
            return _Flags;
        }

        set
        {
            _Flags = value;
        }
    }
}
