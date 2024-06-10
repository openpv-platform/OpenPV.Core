using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Ahsoka.Services.Bluetooth.LE.Bluez.Core;

[DBusInterface("org.bluez.ProfileManager1")]
interface IProfileManager1 : IDBusObject
{
    Task RegisterProfileAsync(ObjectPath Profile, string UUID, IDictionary<string, object> Options);
    Task UnregisterProfileAsync(ObjectPath Profile);
}

[DBusInterface("org.bluez.HealthManager1")]
interface IHealthManager1 : IDBusObject
{
    Task<ObjectPath> CreateApplicationAsync(IDictionary<string, object> Config);
    Task DestroyApplicationAsync(ObjectPath Application);
}

[DBusInterface("org.bluez.Adapter1")]
interface IAdapter1 : IDBusObject
{
    Task StartDiscoveryAsync();
    Task SetDiscoveryFilterAsync(IDictionary<string, object> Properties);
    Task StopDiscoveryAsync();
    Task RemoveDeviceAsync(ObjectPath Device);
    Task<string[]> GetDiscoveryFiltersAsync();
    Task<T> GetAsync<T>(string prop);
    Task<Adapter1Properties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

[DBusInterface("org.bluez.Media1")]
interface IMedia1 : IDBusObject
{
    Task RegisterEndpointAsync(ObjectPath Endpoint, IDictionary<string, object> Properties);
    Task UnregisterEndpointAsync(ObjectPath Endpoint);
    Task RegisterPlayerAsync(ObjectPath Player, IDictionary<string, object> Properties);
    Task UnregisterPlayerAsync(ObjectPath Player);
}

[DBusInterface("org.bluez.NetworkServer1")]
interface INetworkServer1 : IDBusObject
{
    Task RegisterAsync(string Uuid, string Bridge);
    Task UnregisterAsync(string Uuid);
}