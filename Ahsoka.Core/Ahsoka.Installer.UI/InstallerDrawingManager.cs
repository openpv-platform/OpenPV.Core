using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Cairo;
using Ahsoka.Installer.UI.InstallSystem;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.Services.Network;
using Ahsoka.Services.Spark;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility.WiFi;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI;

internal class InstallerDrawingManager : CairoDrawingManager
{
    #region Singleton
    public static InstallerDrawingManager Default { get; } = new();
    static readonly WiFiNetworkManager wifiViewModel = new();
    #endregion

    #region Fields
    InstallServiceClient installClient;
    NetworkServiceClient networkClient;
    SystemServiceClient systemClient;
    #endregion

    #region Properties
    public InstallServiceClient InstallClient { get => installClient; }

    public SystemServiceClient SystemClient { get => systemClient; }

    public NetworkServiceClient NetworkClient { get => networkClient; }

    public WiFiNetworkManager WiFiViewModel { get { return wifiViewModel; } }

    #endregion

    #region Methods
    private InstallerDrawingManager()
    {
        CurrentState = new StartState();
    }

    protected override void OnInit()
    {
        // We Run this Async to allow the UI to show while 
        // we initialize the services which may take some time.
        Task.Run(() =>
        {
            // Start Services to Loader UI
            SystemService systemService = new();
            SparkService sparkService = new();
            NetworkService networkService = new();

            systemService.AutoLoadReceived += SystemService_AutoLoadReceived;

            AhsokaRuntime.Default.ConnectEndPoints(systemService, sparkService, networkService);

            // Start Clients
            installClient = new();
            networkClient = new();
            systemClient = new();

            AhsokaRuntime.Default.ConnectEndPoints(installClient, networkClient, systemClient);

            AhsokaRuntime.ReleaseStartup();

            ((StartState)CurrentState).Init(true);

            Refresh();

            CheckForInternalAutoload();

        }).ContinueWith((result) =>
        {
            wifiViewModel.Init(networkClient);
            wifiViewModel.NetworkListChanged += (o, e) => { Refresh(); };
            wifiViewModel.ConnectionStateChanged += (o, e) => { Refresh(); };
            wifiViewModel.SelectedNetworkChanged += (o, e) => { Refresh(); };
        });
    }

    protected override DrawingTypeface OnCreateTypeFace()
    {
        return DrawingApi.LoadTypeface("Assets/Font.ttf", "Droid Sans");
    }

    private void SystemService_AutoLoadReceived(object sender, LoadApplicationCompleteEventArgs e)
    {
        CurrentState = new InstallingState(e.FileName);
        RequestDrawFrame();
    }

    private void CheckForInternalAutoload()
    {
        string autoloadFile = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder), "autoload" + PackageType.opvEncrypted.GetFileExtension());
        if (File.Exists(autoloadFile))
        {
            CurrentState = new InstallingState(autoloadFile);
            RequestDrawFrame();
        }

        autoloadFile = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder), "autoload.pvx1");
        if (File.Exists(autoloadFile))
        {
            CurrentState = new InstallingState(autoloadFile);
            RequestDrawFrame();
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


