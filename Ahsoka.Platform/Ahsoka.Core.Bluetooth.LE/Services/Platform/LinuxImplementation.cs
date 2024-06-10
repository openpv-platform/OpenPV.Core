using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.LE.Bluez;
using Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements;
using Ahsoka.Services.Bluetooth.LE.Bluez.Core;
using Ahsoka.Services.Bluetooth.LE.Bluez.Gatt;
using Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;
using Ahsoka.Services.Network;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

using DBusBLEAdvertisementProperties = Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements.BLEAdvertisementProperties;

namespace Ahsoka.Services.Bluetooth.LE;

internal class LinuxImplementation : ImplementationBase, IDisposable
{
    #region Fields

    PairingAgent agent;
    BLEService service;
    GattApplicationManager appManager;
    AdvertisingManager adManager;
    string localName, adapterInterfacePath;
    Connection connection = null;
    bool connectionReady = false;
    INetworkFirmwareBase firmware;
    IAdapter1 adapter;

    readonly Dictionary<string, GattCharacteristic> characteristics = new();
    readonly Dictionary<string, GattService> services = new();
    #endregion

    #region Service Methods
    protected override bool OnIsEnabledAdapter(BLEAdapterRequest adapterInfo)
    {
        return connectionReady;
    }

    protected override void OnInit(BLEService service)
    {
        // Get Network Module Manager for Platform.
        firmware = NetworkFirmwareComponent.GetPlatformFirmware(SystemInfo.HardwareInfo);
        if (firmware != null)
            this.service = service;
    }

    protected override void OnEnableAdapter(BLEAdapterRequest adapterInfo)
    {
        if (connection == null)
        {
            localName = adapterInfo.LocalName;
            adapterInterfacePath = SystemInfo.HardwareInfo.BluetoothInfo.BluetoothAdapters.FirstOrDefault()?.DeviceInterfacePath;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: Booting Module if Needed");
            firmware.Init();
            firmware.Boot();

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: Completed - Setting Bluetooth Local Name");
            ProcessUtility.RunProcess("bluetoothctl", $"system-alias {adapterInfo.LocalName}", null, out string result, out string error);

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service: Bluetooth Ready");

            connection = new Connection(Address.System);
            connection.StateChanged += (sender, e) =>
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: StateChanged {e.State}");
                if (e.State == ConnectionState.Connected)
                {
                    // Connect and "Turn On" Adapter. (Not sure if this is really needed.
                    // this is an async call so will send a message when completed
                    adapter = connection.CreateProxy<IAdapter1>("org.bluez", adapterInterfacePath);
                    adapter.SetPoweredAsync(true).ContinueWith((result) =>
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: Notify Ready");

                        // Notify Client of Adapter Ready
                        var response = new BLEConnectionNotification() { State = result.IsFaulted ? BLEConnectionState.ClientNotReady : BLEConnectionState.ClientReady };
                        service.SendNotification(BLEMessageTypes.Ids.AdapterEnabled, response);

                        // Create a App and Service Mangers
                        appManager = new GattApplicationManager(connection, adapterInterfacePath);
                        adManager = new AdvertisingManager(connection, adapterInterfacePath);

                        // Create Pairing Agent for handling Security Requests
                        agent = new PairingAgent(this, connection);

                        connectionReady = true;
                        service.SendNotification(BLEMessageTypes.Ids.AdapterReady, response);
                    });
                }
                else if (e.State == ConnectionState.Disconnected)
                {
                    connection?.Dispose();
                    connection = null;
                    connectionReady = false;

                    // Notify Client of Adapter Ready
                    var response = new BLEConnectionNotification() { State = BLEConnectionState.ClientNotReady };
                    service.SendNotification(BLEMessageTypes.Ids.AdapterDisabled, response);
                }
            };

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: Starting Connect");
            connection.ConnectAsync();
        }
    }

    protected override void OnDisableAdapter(BLEAdapterRequest adapterInfo)
    {
        if (adapter.GetPoweredAsync().GetAwaiter().GetResult())
        {
            adapter.SetPoweredAsync(false).GetAwaiter().GetResult();
            if (adapter.GetPoweredAsync().GetAwaiter().GetResult())
                throw new Exception($"There was a problem trying to disable bluetooth adapter.");

            // Close Connection
            connection.Dispose();
            connection = null;

            // Notify Client of Adapter Ready
            var response = new BLEConnectionNotification() { State = BLEConnectionState.ClientNotReady };
            service.SendNotification(BLEMessageTypes.Ids.AdapterDisabled, response);
        }
    }

    protected override void OnCreateAdvertisement(BLEAdvertisementProperties advertisementProperties)
    {
        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        var list = new List<string>();
        int count = advertisementProperties.ServiceUuids.Count(x => x.Length > 8);
        if (count > 1)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Only 1 128bit UUID May be added to the Advertizing Packet, Only the first UUID was added");
            list.Add(advertisementProperties.ServiceUuids.First());
        }
        else
            list.AddRange(advertisementProperties.ServiceUuids);

        var includes = new List<string> { "tx-power", "local-name" };
        var props = new DBusBLEAdvertisementProperties
        {
            Type = advertisementProperties.Type.ToString().ToLower(),
            ServiceUUIDs = list.ToArray(),
            Includes = includes.ToArray(),
            ManufacturerData = new Dictionary<ushort, object>(),
        };

        foreach (var item in advertisementProperties.ManufactureDatas)
            props.ManufacturerData[(ushort)item.Key] = item.Value;

        adManager.CreateAdvertisement(localName, props).GetAwaiter();
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service:Advertising {adapterInterfacePath}/{localName} Started");
    }

    protected override void OnRemoveAdvertisement(BLEAdvertisementInfo advertisementInfo)
    {
        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        adManager.RemoveAdvertisement(localName).GetAwaiter();
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service:Advertising {adapterInterfacePath}/{localName} Stopped");
    }

    protected override void OnCreateService(BLEServiceProperties serviceProperties)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service:Creating Service {serviceProperties.Uuid.Substring(0, 8)}");

        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        if (services.ContainsKey(serviceProperties.Uuid))
            throw new Exception("This service has already been registered.");

        // Register our Services as an Application
        var task = Task.Run(() =>
        {
            return appManager.RegisterGattApplication(localName, new BLEServiceProperties[1] { serviceProperties }, this);

        }).ContinueWith((result) =>
        {
            foreach (var gattService in result.Result)
            {
                services.Add(gattService.UUID, gattService);

                foreach (var gattCharacteristic in gattService.Characteristics)
                    characteristics[gattCharacteristic.UUID] = gattCharacteristic;

                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service:Service Added {gattService.UUID.Substring(0, 8)} {gattService.ObjectPath}");
            }
        });

        task.Wait();
    }

    protected override void OnRemoveService(BLEServiceInfo serviceInfo)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service:Deleting Service {serviceInfo.Uuid.Substring(0, 8)}");

        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        if (!services.ContainsKey(serviceInfo.Uuid))
            throw new Exception("The service UUID provided couldn't be found and is likely not running.");

        var serviceObjectPath = services[serviceInfo.Uuid].ObjectPath.ToString();
        appManager.UnRegisterGattApplication(serviceObjectPath).GetAwaiter();
    }

    protected override void OnSetCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: Setting Value Service {newValueProperty.Uuid.Substring(0, 8)}");

        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        if (!characteristics.ContainsKey(newValueProperty.Uuid))
            throw new Exception($"The characteristic value {newValueProperty.Uuid} you were trying to set doesn't exist.");

        characteristics[newValueProperty.Uuid].SetCharacteristicValue(newValueProperty);
    }

    protected override void OnQueueCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"BLE Service: Setting Value Service {newValueProperty.Uuid.Substring(0, 8)}");

        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        if (!characteristics.ContainsKey(newValueProperty.Uuid))
            throw new Exception($"The characteristic value {newValueProperty.Uuid} you were trying to queue doesn't exist.");

        characteristics[newValueProperty.Uuid].QueueCharacteristicValue(newValueProperty);
    }

    protected override void OnSendCharacteristicNotification(BLECharacteristicInfo characteristicIdentifier)
    {
        if (!connectionReady)
            throw new Exception("The bluetooth adapter is not enabled and initialized.");

        if (!characteristics.ContainsKey(characteristicIdentifier.Uuid))
            throw new Exception($"The characteristic value {characteristicIdentifier.Uuid} you were trying to notify doesn't exist.");

        characteristics[characteristicIdentifier.Uuid].TriggerNotification();
    }

    protected override void OnConfirmPairing(BLEPairingConfirmation pairingConfirmation)
    {
        agent.ConfirmPairingRequest(pairingConfirmation);
    }

    #endregion

    #region IDisposable
    void IDisposable.Dispose()
    {
        connection.Dispose();
    }
    #endregion

}
