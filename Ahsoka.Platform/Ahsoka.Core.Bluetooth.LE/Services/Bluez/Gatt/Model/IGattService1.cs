using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

[DBusInterface("org.bluez.GattService1")]
internal interface IGattService1 : IDBusObject
{
    Task<object> GetAsync(string prop);
    Task<GattService1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}


[Dictionary]
internal class GattService1Properties
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

    private bool _Primary = default;
    public bool Primary
    {
        get
        {
            return _Primary;
        }

        set
        {
            _Primary = value;
        }
    }

    private ObjectPath[] _Characteristics = default;
    public ObjectPath[] Characteristics
    {
        get
        {
            return _Characteristics;
        }

        set
        {
            _Characteristics = value;
        }
    }
}