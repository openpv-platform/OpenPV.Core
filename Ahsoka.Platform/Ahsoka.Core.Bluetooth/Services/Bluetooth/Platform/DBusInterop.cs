using Ahsoka.ServiceFramework;
using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Bluetooth.Platform;

[ExcludeFromCodeCoverage]
internal class DBusInterop
{
    public static uint AdapterConnectTimeout = 15000;

    readonly List<string> KnownDevices = new();

    protected IReadOnlyList<Adapter> AdapterList = null;

    private static BluetoothService _service;

    private const int SUCCESS = 0;
    private const int ERROR = -1;

    public DBusInterop(BluetoothService service)
    {
        _service = service;
    }

    static BluetoothService GetService()
    {
        return _service;
    }

    internal int ConnectDevice(AhsokaMessageHeader header, int adapterIndex, string deviceAddress, BluetoothProfiles profiles)
    {
        int retval = Task.Run(() => ConnectDeviceAsync(header, adapterIndex, deviceAddress, profiles)).GetAwaiter().GetResult();
        return retval;

    } // end of method ConnectDevice()

    internal async Task<int> ConnectDeviceAsync(AhsokaMessageHeader header, int adapterIndex, string deviceAddress, BluetoothProfiles profiles)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        TimeSpan timeout = TimeSpan.FromSeconds(AdapterConnectTimeout);

        bool connectedState;

        // Get the requested device
        Device device = await adapter.GetDeviceAsync(deviceAddress);

        if (device == null)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Device {deviceAddress} not found!");
            return ERROR;
        } // end of if (device == null)

        if (profiles == BluetoothProfiles.Default)
        {
            // Check if device is already connected
            connectedState = await device.GetConnectedAsync();

            if (connectedState == true)
                return SUCCESS;

            await device.SetTrustedAsync(true);

            await device.ConnectAsync();

            // Check if this device is known (has been connected previously)
            if ((KnownDevices.Count == 0) || !KnownDevices.Contains(deviceAddress))
            {
                // Set up connect/disconnect notifications
                device.Connected += DeviceConnectedEventAsync;
                device.Disconnected += DeviceDisconnectedEventAsync;

                // Add device to known devices
                KnownDevices.Add(deviceAddress);
            }

            connectedState = await device.GetConnectedAsync();

            if (connectedState == true)
                return SUCCESS;
            else
                return ERROR;
        }
        else
        {
            foreach (KeyValuePair<BluetoothProfiles, string> entry in _profile2UUID)
            {
                if ((profiles & entry.Key) == entry.Key)
                {

                    _profile2UUID.TryGetValue(entry.Key, out string uuid);

                    if (uuid == null)
                    {
                        return ERROR;
                    }
                    else
                    {
                        string normalizedUUID = BlueZManager.NormalizeUUID(uuid);
                        await device.ConnectProfileAsync(normalizedUUID);
                        await Task.Delay(500);  // Delay between connection attempts
                    }
                }
            } // end of foreach (KeyValuePair<BluetoothProfiles, string> entry in _profile2UUID)

            // Check if this device is known (has been connected previously)
            if ((KnownDevices.Count == 0) || !KnownDevices.Contains(deviceAddress))
            {
                // Set up connect/disconnect notifications
                device.Connected += DeviceConnectedEventAsync;
                device.Disconnected += DeviceDisconnectedEventAsync;

                // Add device to known devices
                KnownDevices.Add(deviceAddress);
            }

            connectedState = await device.GetConnectedAsync();

            if (connectedState == true)
                return SUCCESS;
            else
                return ERROR;

        } // end of if (profiles == BluetoothProfiles.Default)
    } // end of method ConnectDeviceAsync()

    internal int DisconnectDevice(int adapterIndex, string deviceAddress, BluetoothProfiles profiles)
    {
        int retval = Task.Run(() => DisconnectDeviceAsync(adapterIndex, deviceAddress, profiles)).GetAwaiter().GetResult();
        return retval;

    } // end of method DisconnectDevice()

    internal async Task<int> DisconnectDeviceAsync(int adapterIndex, string deviceAddress, BluetoothProfiles profiles)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        bool connectedState;

        // Get the requested device
        Device device = await adapter.GetDeviceAsync(deviceAddress);

        if (device == null)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Device {deviceAddress} not found!");
            return ERROR;
        } // end of if (device == null)

        // Check if device is already connected
        connectedState = await device.GetConnectedAsync();

        if (connectedState == false)
            return SUCCESS;

        if (profiles == BluetoothProfiles.Default)
        {
            await device.DisconnectAsync();

            connectedState = await device.GetConnectedAsync();

            if (connectedState == false)
                return SUCCESS;
            else
                return ERROR;
        }
        else
        {
            foreach (KeyValuePair<BluetoothProfiles, string> entry in _profile2UUID)
            {
                if ((profiles & entry.Key) == entry.Key)
                {

                    _profile2UUID.TryGetValue(entry.Key, out string uuid);

                    if (uuid == null)
                    {
                        return ERROR;
                    }
                    else
                    {
                        string normalizedUUID = BlueZManager.NormalizeUUID(uuid);
                        await device.DisconnectProfileAsync(normalizedUUID);
                    }
                }
            } // end of foreach (KeyValuePair<BluetoothProfiles, string> entry in _profile2UUID)

            return SUCCESS;
        }

    } // end of method DisconnectDeviceAsync()

    private static async Task DeviceConnectedEventAsync(Device device, BlueZEventArgs e)
    {
        if (e.IsStateChange)
        {
            ProfileConnectionState state = new()
            {
                AdapterIndex = 0,
                DeviceMacAddress = await device.GetAddressAsync(),
                Profile = BluetoothProfiles.Default,
                IsConnected = await device.GetConnectedAsync(),
            };

            GetService().SendProfileConnectionStateChangedNotification(state);
        }
    } // end of Task DeviceConnectedEventAsync()

    private static async Task DeviceDisconnectedEventAsync(Device device, BlueZEventArgs e)
    {
        if (e.IsStateChange)
        {
            ProfileConnectionState state = new()
            {
                AdapterIndex = 0,
                DeviceMacAddress = await device.GetAddressAsync(),
                Profile = BluetoothProfiles.Default,
                IsConnected = await device.GetConnectedAsync(),
            };

            GetService().SendProfileConnectionStateChangedNotification(state);
        }
    }// end of Task DeviceDisconnectedEventAsync()

    internal BluetoothDevicePairedInfo PairDevice(int adapterIndex, string deviceAddress)
    {
        BluetoothDevicePairedInfo retval = Task.Run(() => PairDeviceAsync(adapterIndex, deviceAddress)).GetAwaiter().GetResult();
        return retval;

    } // end of method PairDevice()

    internal async Task<BluetoothDevicePairedInfo> PairDeviceAsync(int adapterIndex, string deviceAddress)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        BluetoothDevicePairedInfo pairedInfo = new()
        {
            AdapterIndex = adapterIndex,
            DeviceMacAddress = deviceAddress,
            DeviceRssi = 0,
            DeviceName = "",
            Paired = false
        };

        // Get the requested device
        Device device = await adapter.GetDeviceAsync(deviceAddress);

        if (device == null)
        {
            uint discoverTimeout = await adapter.GetDiscoverableTimeoutAsync();

            await adapter.StartDiscoveryAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(discoverTimeout));
            await adapter.StopDiscoveryAsync();

            device = await adapter.GetDeviceAsync(deviceAddress);

            if (device == null)
                return pairedInfo;

        } // end of if (device == null)

        bool paired = await device.GetPairedAsync();

        if (paired == false)
        {
            uint pairTimeout = await adapter.GetPairableTimeoutAsync();

            TimeSpan timeout = TimeSpan.FromMilliseconds(pairTimeout);

            await device.SetTrustedAsync(true);
            await device.PairAsync();
            await device.WaitForPropertyValueAsync("Paired", value: true, timeout);
        }

        Device1Properties props = await device.GetAllAsync();

        pairedInfo.DeviceRssi = props.RSSI;
        pairedInfo.DeviceName = props.Alias;
        pairedInfo.Paired = props.Paired;

        return pairedInfo;

    } // end of method PairDeviceAsync()

    internal BluetoothPairedDevices GetPairedDevices(int adapterIndex)
    {
        BluetoothPairedDevices retval = Task.Run(() => GetPairedDevicesAsync(adapterIndex)).GetAwaiter().GetResult();
        return retval;

    } // end of method GetPairedDevices()

    internal async Task<BluetoothPairedDevices> GetPairedDevicesAsync(int adapterIndex)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        IReadOnlyList<Device> pairedList;

        pairedList = await adapter.GetDevicesAsync();

        BluetoothPairedDevices pairedDevices = new()
        {
            AdapterIndex = adapterIndex
        };

        foreach (var pairedDevice in pairedList)
        {
            Device1Properties props;

            BluetoothDevicePairedInfo deviceInfo = new();

            props = await pairedDevice.GetAllAsync();

            if (props.Paired)
            {
                deviceInfo.AdapterIndex = adapterIndex;
                deviceInfo.DeviceRssi = props.RSSI;
                deviceInfo.DeviceMacAddress = props.Address;
                deviceInfo.DeviceName = props.Alias;
                deviceInfo.Paired = props.Paired;

                pairedDevices.PairedDeviceLists.Add(deviceInfo);
            }
        }

        return pairedDevices;

    } // end of method GetPairedDevicesAsync()

    internal int RemoveDevice(int adapterIndex, string cmdDeviceAddr)
    {
        int retval = Task.Run(() => RemoveDeviceAsync(adapterIndex, cmdDeviceAddr)).GetAwaiter().GetResult();
        return retval;

    } // end of method RemoveDevice()

    internal async Task<int> RemoveDeviceAsync(int adapterIndex, string cmdDeviceAddr)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        string adapterPath = adapter!.ObjectPath.ToString();
    
        // Build the device path
        string path = adapterPath + "/dev_" + cmdDeviceAddr.Replace(':', '_');

        // Ensure adapter is powered
        Adapter1Properties adapterProperties1 = await adapter!.GetAllAsync();

        if (adapterProperties1.Powered == false)
        {
            await adapter.SetPoweredAsync(true);
            await Task.Delay(1000);
        }

        await adapter.RemoveDeviceAsync(path);

        return SUCCESS;

    } // end of method RemoveDeviceAsync()

    internal void SetAdapterConfig(int adapterIndex, BluetoothAdapterSetup adapterConfiguration)
    {
        Task.Run(() => SetAdapterConfigAsync(adapterIndex, adapterConfiguration)).Wait();

    } // end of method GetPairedDevices()

    internal async void SetAdapterConfigAsync(int adapterIndex, BluetoothAdapterSetup adapterConfiguration)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // [Function Invocation Side Effect] Update adapter configuration
        adapterConfiguration.DeviceDiscoveryName = await adapter.GetNameAsync();
        adapterConfiguration.DeviceMacAddress = await adapter.GetAddressAsync();

        // Set adapter
        await adapter.SetDiscoverableTimeoutAsync((uint)adapterConfiguration.DiscoveryTimeout);
        await adapter.SetPairableTimeoutAsync((uint)adapterConfiguration.PairingTimeout);

        // Set the adapter connection timeout
        AdapterConnectTimeout = (uint)adapterConfiguration.ConnectionTimeout;

    } // end of method SetAdapterConfigAsync()

    internal bool GetAdapterDiscoverable(int adapterIndex)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        bool retval = Task.Run(async () => await adapter.GetDiscoverableAsync()).GetAwaiter().GetResult();
        return retval;

    } // end of method GetAdapterDiscoverable()

    internal void SetAdapterDiscoverable(int adapterIndex, bool state)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Set adapter discoverable state
        Task.Run(async () => await adapter.SetDiscoverableAsync(state)).Wait();

    } // end of method SetAdapterDiscoverable()

    internal void setAdapterDiscoverable(Adapter adapter, bool state)
    {
        // Set adapter discoverable state
        Task.Run(async () => await adapter.SetDiscoverableAsync(state)).Wait();
    } // end of method setAdapterDiscoverable()

    internal uint GetAdapterDiscoverableTimeout(int adapterIndex)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Get adapter discoverable state 
        uint retval = Task.Run(async () => await adapter.GetDiscoverableTimeoutAsync()).GetAwaiter().GetResult();

        return retval;

    } // end of method GetAdapterDiscoverableTimeout()

    internal void SetAdapterDiscoverableTimeout(int adapterIndex, uint timeout)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Set adapter discoverable state
        Task.Run(async () => await adapter.SetDiscoverableTimeoutAsync(timeout)).Wait();

    } // end of method SetAdapterDiscoverableTimeout()

    internal bool GetAdapterPairable(int adapterIndex)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Get adapter discoverable state 
        bool retval = Task.Run(async () => await adapter.GetPairableAsync()).GetAwaiter().GetResult();

        return retval;

    } // end of method GetAdapterPairable()

    internal void SetAdapterPairable(int adapterIndex, bool state)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Set adapter discoverable state
        Task.Run(async () => await adapter.SetPairableAsync(state)).Wait();

    } // end of method SetAdapterPairable()

    internal uint GetAdapterPairableTimeout(int adapterIndex)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Get adapter discoverable state 
        uint retval = Task.Run(async () => await adapter.GetPairableTimeoutAsync()).GetAwaiter().GetResult();

        return retval;

    } // end of method GetAdapterPairableTimeout()

    internal void SetAdapterPairableTimeout(int adapterIndex, uint timeout)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Set adapter discoverable state
        Task.Run(async () => await adapter.SetPairableTimeoutAsync(timeout)).Wait();

    } // end of method SetAdapterPairableTimeout()

    internal bool GetAdapterPowered(int adapterIndex)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Get adapter discoverable state 
        bool retval = Task.Run(async () => await adapter.GetPoweredAsync()).GetAwaiter().GetResult();

        return retval;

    } // end of method GetAdapterPower()

    internal void SetAdapterPowered(int adapterIndex, bool state)
    {
        Adapter adapter = GetAdapter(adapterIndex);

        // Set adapter discoverable state
        Task.Run(async () => await adapter.SetPoweredAsync(state)).Wait();

    } // end of method SetAdapterPower()

    // The device discovery logic uses the _discoveryDevices list as a means to collect and report on
    //   the asynchronously discovered devices over the last one second time interval. This continues
    //   every second for the duration of the discovery (scan) period. A semaphore is used to avoid
    //   the asynchronous scanning task (ExecDiscovery) from colliding with the reporting task
    //   (GetDiscoveredDevicesInfo).
    readonly IList<BluetoothDeviceDiscoveryInfo> DiscoveryDevices = new List<BluetoothDeviceDiscoveryInfo>();

    private readonly Semaphore _discoveryDevicesSemaphore = new(1, 1);

    internal IEnumerable<BluetoothDeviceDiscoveryInfo> GetDiscoveredDevicesInfo(int adapterIndex, int scanMilliseconds)
    {
        DiscoveryDevices.Clear();

        ExecDiscovery(adapterIndex, scanMilliseconds);

        for (int i = 0; i < (scanMilliseconds + 1000) / 1000; i++)
        {
            int count;

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();

            _discoveryDevicesSemaphore.WaitOne();
            count = DiscoveryDevices.Count;
            _discoveryDevicesSemaphore.Release();

            if (count == 0)
                continue;

            _discoveryDevicesSemaphore.WaitOne();
            foreach (BluetoothDeviceDiscoveryInfo info1 in DiscoveryDevices)
            {
                yield return info1;
            }
            DiscoveryDevices.Clear();
            _discoveryDevicesSemaphore.Release();
        }
    } // end of method GetDiscoveredDevicesInfo()

    internal void ExecDiscovery(int adapterIndex, int scanMilliseconds)
    {
        Task.Run(() => ExecDiscoveryAsync(adapterIndex, scanMilliseconds)).Wait();

    } // end of method ExecDiscovery()

    internal async void ExecDiscoveryAsync(int adapterIndex, int scanMilliseconds)
    {
        IReadOnlyList<Device> preScanDevices;

        Adapter adapter = GetAdapter(adapterIndex);

        // Get known pre-scan devices (these are paired devices)
        preScanDevices = await adapter.GetDevicesAsync();

        // Gain access to the _discoveryDevices list and add the pre-scan devices.
        _discoveryDevicesSemaphore.WaitOne();
        foreach (var preDevice in preScanDevices)
        {
            BluetoothDeviceDiscoveryInfo preInfo = new();

            Device1Properties preProps = await preDevice.GetAllAsync();

            preInfo.AdapterIndex = adapterIndex;
            preInfo.DeviceName = preProps.Alias;
            preInfo.DeviceMacAddress = preProps.Address;
            preInfo.DeviceRssi = preProps.RSSI;

            DiscoveryDevices.Add(preInfo);
        }
        _discoveryDevicesSemaphore.Release();

        using (adapter.WatchDevicesAddedAsync(async device =>
        {
            BluetoothDeviceDiscoveryInfo info = new();

            Device1Properties props = await device.GetAllAsync();

            info.AdapterIndex = adapterIndex;
            info.DeviceName = props.Alias;
            info.DeviceMacAddress = props.Address;
            info.DeviceRssi = props.RSSI;

            _discoveryDevicesSemaphore.WaitOne();
            DiscoveryDevices.Add(info);
            _discoveryDevicesSemaphore.Release();

        }).GetAwaiter().GetResult())
        {
            await adapter.StartDiscoveryAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(scanMilliseconds));
            await adapter.StopDiscoveryAsync();
        }
    } // end of method doDiscovery()

    internal void InitAdapterList()
    {
        Task<IReadOnlyList<Adapter>> adaptersTask;

        if (AdapterList == null)
        {
            // Create the adapter list
            AdapterList = new List<Adapter>();

            // Get a list of the available bluetooth adapters
            adaptersTask = Task.Run(async () => await BlueZManager.GetAdaptersAsync());

            if ((adaptersTask == null) || (adaptersTask.Result == null) || (adaptersTask.Result.Count == 0))
                return;

            AdapterList = adaptersTask.Result;

        } // end of if (_adapterList == null)

    } // end of method InitAdapterList()

    private Adapter GetAdapter(int adapterIndex)
    {
        if ((AdapterList == null) || (AdapterList.Count == 0))
            throw new ArgumentOutOfRangeException("No adapters found");

        if (adapterIndex >= AdapterList.Count)
            throw new ArgumentOutOfRangeException("Parameter adapterIndex is out of range");

        Adapter adapter = AdapterList[adapterIndex];

        return adapter;

    } // end of method GetAdapter()

    internal int GetAdapterListCount()
    {
        if (AdapterList == null)
            return 0;

        return AdapterList.Count;

    } // end of method GetAdapterListCount()

    private static readonly IDictionary<BluetoothProfiles, string> _profile2UUID = new Dictionary<BluetoothProfiles, string>()
    {
        { BluetoothProfiles.Spp,          "1101" },     // Serial Port Profile
        { BluetoothProfiles.HfpAg,        "111f" },     // Handsfree Audio Gateway Profile
        { BluetoothProfiles.Hfp,          "111e" },     // Handsfree Profile
        { BluetoothProfiles.Opp,          "1105" },     // Object Push Profile
        { BluetoothProfiles.A2dpSink,     "110b" },     // Advanced Audio Distribution Profile Sink
        { BluetoothProfiles.A2dpSource,   "110a" },     // Advanced Audio Distribution Profile Source
        { BluetoothProfiles.AvrcpSource,  "110e" },     // Audio/Video Remote Control Profile Source
        { BluetoothProfiles.AvrcpTarget,  "110c" },     // Audio/Video Remote Control Profile Target
        { BluetoothProfiles.Hid,          "1124" },     // Human Interface Device Profile
        { BluetoothProfiles.HspAg,        "1112" },     // Headset Audio Gateway Profile
        { BluetoothProfiles.Hsp,          "1108" },     // Headset Profile
        { BluetoothProfiles.HdpSink,      "1402" },     // Health Device Profile Sink
        { BluetoothProfiles.HdpSource,    "1401" },     // Health Device Profile Source
        { BluetoothProfiles.Pbap,         "1130" },     // Phone Book Access Profile
        { BluetoothProfiles.Bgio,         "111a" },     // Basic Imaging Profile ???
        { BluetoothProfiles.Ota,          "111e" },     // Object Transfer???
        { BluetoothProfiles.Map,          "1134" },     // Message Access Profile
        { BluetoothProfiles.Iap,          "111e" },     // iPod Accessory Pofile ???
    };

} // end of internal class DBusInterop