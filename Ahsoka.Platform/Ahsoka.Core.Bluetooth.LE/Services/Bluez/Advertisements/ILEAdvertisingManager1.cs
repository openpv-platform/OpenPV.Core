using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements;

[DBusInterface("org.bluez.LEAdvertisingManager1")]
interface ILEAdvertisingManager1 : IDBusObject
{
    Task RegisterAdvertisementAsync(ObjectPath Advertisement, IDictionary<string, object> Options);
    Task UnregisterAdvertisementAsync(ObjectPath Service);
    Task<T> GetAsync<T>(string prop);
    Task<LEAdvertisingManager1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
