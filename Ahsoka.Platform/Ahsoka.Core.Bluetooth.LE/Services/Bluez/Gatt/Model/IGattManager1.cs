using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluez.Gatt.Model;

[DBusInterface("org.bluez.GattManager1")]
interface IGattManager1 : IDBusObject
{
    Task RegisterApplicationAsync(ObjectPath Application, IDictionary<string, object> Options);
    Task UnregisterApplicationAsync(ObjectPath Application);
}
