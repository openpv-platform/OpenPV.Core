using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluez;

[DBusInterface("org.bluez.AgentManager1")]
interface IAgentManager1 : IDBusObject
{
    Task RegisterAgentAsync(ObjectPath Agent, string Capability);
    Task UnregisterAgentAsync(ObjectPath Agent);
    Task RequestDefaultAgentAsync(ObjectPath Agent);
}
