using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Utility;
using Ahsoka.Installer.UI.InstallSystem;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.Install;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility.WiFi;
using System;
using System.IO;
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
    #endregion

    #region Properties
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
                // Start Clients
                installClient = new();
                systemClient = new();
                deviceClient = new();
                AhsokaRuntime.Default.StartEndPoints(installClient, systemClient, deviceClient);

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
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Startup Exception " + ex.Message +  ex.StackTrace);
            }

        }).ContinueWith((result) =>
        {
            wifiViewModel.AdapterEnabledChanged += (o, e) => { Invalidate(); };
            wifiViewModel.NetworkListChanged += (o, e) => { Invalidate(); };
            wifiViewModel.ConnectionStateChanged += (o, e) => { Invalidate(); };
            wifiViewModel.SelectedNetworkChanged += (o, e) => { Invalidate(); };

            // Start Clients....These Use AutoStart
            wifiViewModel.Start(deviceClient);
        });
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
                DrawingApi.DrawText($"WiFi: {wifiViewModel.SelectedNetwork?.Ssid} @ {info.IPAddress}", width - 10, height - 8, textInfo);
            }
            else if (wifiViewModel.ConnectionState != WirelessConnectionState.ClientDisconnected)
                DrawingApi.DrawText($"WiFi: {wifiViewModel.SelectedNetwork?.Ssid}", width - 10, height - 8, textInfo);
            else
                DrawingApi.DrawText($"WiFi: None", width - 10, height - 8, textInfo);
        }
    }

    #endregion
}


