using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Core;

[DBusInterface("org.freedesktop.DBus.ObjectManager")]
interface IObjectManager : IDBusObject
{
    Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
}
