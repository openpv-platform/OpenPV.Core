using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

[DBusInterface("org.bluez.GattCharacteristic1")]
interface IGattCharacteristic1 : IDBusObject
{
    Task<byte[]> ReadValueAsync(IDictionary<string, object> Options);
    Task WriteValueAsync(byte[] Value, IDictionary<string, object> Options);
    Task StartNotifyAsync();
    Task StopNotifyAsync();
    Task<object> GetAsync(string prop);
    Task<GattCharacteristic1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}


[Dictionary]
internal class GattCharacteristic1Properties
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

    private ObjectPath _Service = default;
    public ObjectPath Service
    {
        get
        {
            return _Service;
        }

        set
        {
            _Service = value;
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

    private bool _Notifying = default;
    public bool Notifying
    {
        get
        {
            return _Notifying;
        }

        set
        {
            _Notifying = value;
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