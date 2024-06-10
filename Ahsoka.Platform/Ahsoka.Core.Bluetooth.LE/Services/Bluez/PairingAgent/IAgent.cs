using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez;

/// <summary>
/// Agent Proxy
/// </summary>
[DBusInterface("org.bluez.Agent1")]
interface IAgent : IDBusObject, IDisposable
{
    Task ReleaseAsync();
    Task<string> RequestPinCodeAsync(ObjectPath device);
    Task DisplayPinCodeAsync(ObjectPath device, string pincode);
    Task<uint> RequestPasskeyAsync(ObjectPath device);
    Task DisplayPasskeyAsync(ObjectPath device, uint passkey, ushort entered);
    Task RequestConfirmationAsync(ObjectPath device, uint passkey);
    Task RequestAuthorizationAsync(ObjectPath device);
    Task AuthorizeServiceAsync(ObjectPath device, string uuid);
    Task CancelAsync();
}

//<!--for /org/bluez
//<node>
//	<interface name="org.freedesktop.DBus.Introspectable">
//		<method name = "Introspect" >
//            <arg name="xml" type="s" direction="out"/>
//		</method>
//	</interface>
//	<interface name="org.bluez.AgentManager1">
//		<method name = "RegisterAgent" >
//          <arg name="agent" type="o" direction="in"/>
//			<arg name = "capability" type="s" direction="in"/>
//		</method>
//		<method name = "UnregisterAgent" >
//            <arg name="agent" type="o" direction="in"/>
//		</method>
//		<method name = "RequestDefaultAgent" >
//            <arg name="agent" type="o" direction="in"/>
//		</method>
//	</interface>
//	<interface name="org.bluez.ProfileManager1">
//		<method name = "RegisterProfile" >
//            <arg name="profile" type="o" direction="in"/>
//			<arg name = "UUID" type="s" direction="in"/>
//			<arg name = "options" type="a{sv}" direction="in"/>
//		</method>
//		<method name = "UnregisterProfile" >
//            <arg name="profile" type="o" direction="in"/>
//		</method>
//	</interface>
//	<node name = "hci0" />
//</ node>