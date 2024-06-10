using Ahsoka.Services.Bluetooth.LE.Bluez.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

[DBusInterface("org.freedesktop.DBus.ObjectManager")]
internal class GattApplication : IObjectManager
{
    private readonly IList<GattService> services = new List<GattService>();

    public GattApplication(ObjectPath objectPath)
    {
        ObjectPath = objectPath;
    }

    public ObjectPath ObjectPath { get; }

    public Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync()
    {
        IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> result =
            new Dictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>();

        foreach (var service in services)
        {
            result[service.ObjectPath] = service.GetProperties();

            foreach (var characteristic in service.Characteristics)
            {
                result[characteristic.ObjectPath] = characteristic.GetProperties();
                foreach (var descriptor in characteristic.Descriptors)
                {
                    result[descriptor.ObjectPath] = descriptor.GetProperties();
                }
            }
        }

        return Task.FromResult(result);
    }

    public GattService AddService(GattService1Properties gattService)
    {
        var servicePath = ObjectPath + "/service" + services.Count;
        var service = new GattService(servicePath, gattService);
        services.Add(service);
        return service;
    }
}