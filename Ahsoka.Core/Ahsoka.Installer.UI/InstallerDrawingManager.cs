using Ahsoka.Core;
using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Utility;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility.WiFi;
using Ahsoka.Installer.UI.InstallSystem;
using Ahsoka.Services.Device;
using Ahsoka.Services.Install;
using Ahsoka.Services.Network;
using Ahsoka.Services.Spark;
using Ahsoka.Services.System;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI;

internal class InstallerDrawingManager : SkiaDrawingManager
{
    #region Singleton
    public static InstallerDrawingManager Default { get; } = new();
    static readonly WiFiNetworkManager wifiViewModel = new();
    #endregion

    #region Fields
    InstallServiceClient installClient;
    DeviceServiceClient deviceClient;
    SystemServiceClient systemClient;
    SparkService sparkService;
    CellularServiceClient cellularServiceClient;
    #endregion

    #region Properties
    public bool HasCellModem { get; set; }
    public bool HasEthernet { get; set; }

    public InstallServiceClient InstallClient { get => installClient; }

    public SystemServiceClient SystemClient { get => systemClient; }

    public DeviceServiceClient DeviceClient { get => deviceClient; }

    public WiFiNetworkManager WiFiViewModel { get { return wifiViewModel; } }

    #endregion

    #region Methods
    private InstallerDrawingManager() : base("Ahsoka.Installer", 1000)
    {
        CurrentState = new StartState();
    }

    protected override void OnInit()
    {
        // We Run this Async to allow the UI to show while 
        // we initialize the services which may take some time.
        Task.Run(() =>
        {
            try
            {
                // Start Services
                sparkService = new();
                sparkService.Start();

                // Start Clients
                installClient = new();
                systemClient = new();
                deviceClient = new();
                cellularServiceClient = new();
                deviceClient.NotificationReceived += DeviceClient_NotificationReceived;

                AhsokaRuntime.Default.StartEndPoints(installClient, systemClient, deviceClient, cellularServiceClient);

                var systemService = AhsokaRuntime.Default.GetService(systemClient.ServiceConfig.ServiceName) as SystemService;
                systemService.AutoLoadReceived += SystemService_AutoLoadReceived;

                ((StartState)CurrentState).Init(true);
                Invalidate();

                // Release Startup and UX
                ApplicationContext.ReleaseStartup();

                CheckForInternalAutoload();
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Startup Exception " + ex.Message + ex.StackTrace);
            }

        }).ContinueWith((result) =>
        {
            wifiViewModel.AdapterEnabledChanged += (o, e) => { Invalidate(); };
            wifiViewModel.NetworkListChanged += (o, e) => { Invalidate(); };
            wifiViewModel.ConnectionStateChanged += (o, e) => { Invalidate(); };
            wifiViewModel.SelectedNetworkChanged += (o, e) => { Invalidate(); };

            // Start Clients....These Use AutoStart
            wifiViewModel.Start(deviceClient);

            // NOTE We start this after the wifi model so it recieves notifications
            deviceClient.EnableDeviceNotifications();
        });
    }

    protected override void OnClose(int result = 0)
    {
        if (HasCellModem)
            StopNetwork();

        base.OnClose(result);
    }

    private void CellClient_NotificationReceived(object sender, AhsokaClientBase<CellularMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == CellularMessageTypes.Ids.NetworkStateChanged)
        {
            CellularNetworkInformation info = e.NotificationObject as CellularNetworkInformation;
            Console.WriteLine($"Network Service Status: {info.ConnectionState}");
            Console.WriteLine($"    IPAddress: {info.IpInfo?.IpPAddress}");
            Console.WriteLine($"    Service: {info.ServiceName}");
            Console.WriteLine($"    Network: {info.NetworkOperatorName}");
            Console.WriteLine($"    RSSI: {info.Rssi} SNR: {info.Snr}");

            this.HasCellModem = info.ConnectionState == CellularNetworkState.Connected;
            this.Invalidate();
        }
    }

    private void DeviceClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.DeviceEnabled)
        {
            DeviceInfo info = e.NotificationObject as DeviceInfo;
            if (info.DeviceType == DeviceTypes.UsbEthernet)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"USB Ethernet Ready");
                HasEthernet = true;
                this.Invalidate();
            }
            else if (info.DeviceType == DeviceTypes.UsbCellularModule)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Cellular Adapter Enabled - Starting Cellular Setup");

                cellularServiceClient.NotificationReceived += CellClient_NotificationReceived;

                // Find Adapter that Supports This Service
                var adapterInfo = cellularServiceClient.GetAdapters().Adapters.FirstOrDefault();
                if (adapterInfo != null)
                    cellularServiceClient.StartNetwork(new() { AdapterId = adapterInfo.AdapterId, Apn = "hologram" });

                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Cellular Service Started");

            }
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.DeviceDisabled)
        {
            DeviceInfo info = e.NotificationObject as DeviceInfo;
            if (info.DeviceType == DeviceTypes.UsbEthernet)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"USB Ethernet Removed");
                HasEthernet = false;
                this.Invalidate();
            }
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceAdded)
        {
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;
            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularModule)
            {
                var deviceList = deviceClient.GetDevices(new DeviceInfoRequest() { FeatureSearch = SupportedFeatures.Cellular });
                if (deviceList.Devices.Count > 0)
                {
                    var device = deviceList.Devices.First();
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Cellular Device Added - Enabling {device.Name}");
                    deviceClient.EnableDevice(device);
                }
            }
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.UsbDeviceRemoved)
        {
            UsbDeviceInfo info = e.NotificationObject as UsbDeviceInfo;
            if (info.DeviceInfo.DeviceType == DeviceTypes.UsbCellularModule)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Cellular Device Removed - Disabling");

                StopNetwork();

                this.HasCellModem = false;
                this.Invalidate();

            }
        }
    }

    private void StopNetwork()
    {
        // Find Adapter that Supports This Service
        var adapterInfo = cellularServiceClient.GetAdapters().Adapters.FirstOrDefault();
        if (adapterInfo != null)
            cellularServiceClient.StopNetwork(new() { AdapterId = adapterInfo.AdapterId });

        var deviceList = deviceClient.GetDevices(new DeviceInfoRequest() { FeatureSearch = SupportedFeatures.Cellular });
        if (deviceList.Devices.Count > 0)
        {
            var device = deviceList.Devices.First();
            deviceClient.DisableDevice(device);
        }

        cellularServiceClient.NotificationReceived -= CellClient_NotificationReceived;
    }

    protected override DrawingTypeface OnCreateTypeFace()
    {
        return DrawingApi.LoadTypeface("Assets/Font.ttf", "Droid Sans");
    }

    private void SystemService_AutoLoadReceived(object sender, LoadApplicationCompleteEventArgs e)
    {
        CurrentState = new InstallingState(e.FileName);
    }

    private void CheckForInternalAutoload()
    {
        string autoloadFile = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder), "autoload" + PackageType.opvEncrypted.GetFileExtension());
        if (File.Exists(autoloadFile))
        {
            CurrentState = new InstallingState(autoloadFile);
        }

        autoloadFile = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder), "autoload.pvx1");
        if (File.Exists(autoloadFile))
        {
            CurrentState = new InstallingState(autoloadFile);
        }
    }
    #endregion

    #region Drawing Methods
    protected override void OnDrawBackground()
    {
        int width = DrawingApi.ScreenWidth;
        int height = DrawingApi.ScreenHeight;

        if (wifiViewModel.AdapterEnabled)
        {
            DrawingTextInfo textInfo = new() { Color = DrawingColors.Black, TextSize = 16, Alignment = DrawingTextAlignment.Left, Typeface = UITypeface };
            DrawingApi.DrawText($"Status: {wifiViewModel.ConnectionState}", 10, height - 8, textInfo);
            textInfo.Alignment = DrawingTextAlignment.Right;

            if (wifiViewModel.ConnectionState == WirelessConnectionState.ClientConnected)
            {
                var info = wifiViewModel.GetIPInfo();
                DrawingApi.DrawText($"WiFi: {wifiViewModel.SelectedNetwork?.Ssid} @ {info.IpPAddress}", width - 10, height - 8, textInfo);
            }
            else if (wifiViewModel.ConnectionState != WirelessConnectionState.ClientDisconnected)
                DrawingApi.DrawText($"WiFi: {wifiViewModel.SelectedNetwork?.Ssid}", width - 10, height - 8, textInfo);
            else
                DrawingApi.DrawText($"WiFi: None", width - 10, height - 8, textInfo);
        }
    }

    #endregion
}


