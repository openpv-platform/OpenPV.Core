using Ahsoka.DeveloperTools.Views;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Security;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Media;
using Renci.SshNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Ahsoka.DeveloperTools.ViewModel;

internal class ActiveConnectionViewModel : ViewModelBase
{
    #region Connection Management Fields
    readonly Timer timer = new(5000);
    readonly object syncRoot = new();
    GetHardwareInformationReply hardwareInfo;
    SystemServiceClient client;
    SecurityServiceClient securityClient;
    ConnectionInfo selectedInfo;
    bool isConnected = false;
    bool isInited = false;
    bool isInstaller = false;
    string connectionString = string.Empty;
    private readonly MainViewModel parentViewModel;

    public ActiveConnectionViewModel(MainViewModel mainViewModel)
    {
        this.parentViewModel = mainViewModel;
    }
    #endregion

    #region Properties
    public bool IsConnected
    {
        get { return isConnected; }
        set
        {
            isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectionColor));
            UpdateConnectedDescription();
        }
    }

    public bool IsInstaller
    {
        get { return isInstaller; }
        set
        {
            isInstaller = value;
            OnPropertyChanged();
        }
    }

    public GetHardwareInformationReply ConnectedHardwareInfo
    {
        get { return hardwareInfo; }
        set
        {
            hardwareInfo = value;
            OnPropertyChanged();
        }
    }

    public ConnectionInfo Connection
    {
        get { return selectedInfo; }
        set
        {
            selectedInfo = value;
            OnPropertyChanged();
            if (value != null)
                Reconnect(value);
        }
    }

    public string ConnectionDescription
    {
        get { return connectionString; }
        set { connectionString = value; OnPropertyChanged(); }
    }

    public ISolidColorBrush ConnectionColor
    {
        get { return IsConnected ? Brushes.SteelBlue : Brushes.Gray; }
        set { }
    }
    #endregion

    #region Methods
    public void Init()
    {
        // Create Connection
        lock (syncRoot)
            StartConnection(Connection);

        // Start Timer to Listen
        timer.Elapsed += Timer_Tick;
        timer.Start();

        isInited = true;
    }

    private void StartConnection(ConnectionInfo value)
    {
        lock (syncRoot)
        {
            var config = new ServiceConfiguration()
            {
                ServiceName = "SystemService",
                TcpConnectionAddress = value.HostName,
                DataChannel = value.SystemServicePort,
            };
            var securityConfig = new ServiceConfiguration()
            {
                ServiceName = "SecurityService",
                TcpConnectionAddress = value.HostName,
                DataChannel = value.SecurityServicePort,
            };

            if (client == null)
            {
                client = new(config);
                securityClient = new(securityConfig);

                UpdateConnectedDescription();
                AhsokaRuntime.Default.StartEndPoints(client);
            }
            else
            {
                UpdateConnectedDescription();
                AhsokaRuntime.Default.StopEndPoints(client);

                client = new(config);
                securityClient = new(securityConfig);

                AhsokaRuntime.Default.StartEndPoints(client);
            }
        }
    }

    private void Reconnect(ConnectionInfo value)
    {
        if (!isInited)
            return;

        lock (syncRoot)
        {
            StartConnection(value);
        }
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        try
        {
            timer.Stop();

            lock (syncRoot)
            {
                AttemptGetHardware();

                // Failed Message....Atttempt to Reconnect.
                if (hardwareInfo == null)
                    Reconnect(Connection);
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("InvalidToken"))
            {
                var request = securityClient.RequestToken(new TokenRequestInfo()
                {
                    ApiKey = Connection.APIKey,
                    ClientIds = { ((IAhsokaClientEndPoint)client).GetClientId() },
                    ClientDescription = "OpenPV Desktop"
                });

                // Waits for Accepted Token to be Recieved
                var systemServiceToken = securityClient.WaitForToken(request.RequestId);
                if (systemServiceToken == null || !systemServiceToken.TokenIsValid)
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, "The token was not valid");
                else
                    client.SetToken(systemServiceToken);

                AttemptGetHardware();
            }
            else
            {
                Debug.WriteLine(e.ToString());
                IsConnected = AttemptGetHardware();
            }
        }
        finally
        {
            timer.Start();
        }
    }

    private bool AttemptGetHardware()
    {
        ConnectedHardwareInfo = client.GetHardwareInformation(2000);

        // Clear any Line Endings
        if (ConnectedHardwareInfo != null)
        {
            var items = ConnectedHardwareInfo.VersionInfoes.ToArray();
            ConnectedHardwareInfo.VersionInfoes.Clear();
            foreach (var item in items)
                ConnectedHardwareInfo.VersionInfoes[item.Key] = item.Value.ReplaceLineEndings("");
        }

        IsConnected = hardwareInfo != null;
        IsInstaller = hardwareInfo != null && hardwareInfo.IsInstaller;

        return IsConnected;
    }

    internal GetHardwareInformationReply GetHardwareInformation(int timeoutInMs = 2000)
    {
        lock (syncRoot)
            return client.GetHardwareInformation(timeoutInMs);
    }

    internal Task<MessageReplyStatus> LoadApplicationAsync(FileStream stream, bool autoLoad, Progress<FileTransmitProgressInfo> progressString)
    {
        lock (syncRoot)
            return client.LoadApplicationAsync(stream, autoLoad, progressString);
    }

    private void UpdateConnectedDescription()
    {
        var connection = parentViewModel.Connections.ConnectionList.FirstOrDefault(x => x.ConnectionAddress == Connection.ConnectionAddress);

        string isConnectedString = IsConnected ? "Connected" : "Waiting for Connection";
        string appName = hardwareInfo != null ? $"({hardwareInfo?.ApplicationName})" : "";

        if (connection != null)
            ConnectionDescription = $"{isConnectedString} to {connection.Description} {appName}";
        else
            ConnectionDescription = $"{isConnectedString} to {Connection.ConnectionAddress} {appName}";
    }

    internal void CancelLoadApplication()
    {
        client.CancelLoadApplication();
    }

    internal void RebootDisplay()
    {
        parentViewModel.Console.ClearConsole();

        parentViewModel.StartProcess();
        IsConnected = false;
        Task.Run(() =>
        {
            if (isConnected)
            {
                lock (syncRoot)
                    client.RequestServiceShutdown(ShutdownTypes.Reboot);
            }
            else
            {
                var prep = RemoteTargetToolFactory.GetToolsForPlatform(this.Connection.PlatformFamily);
                prep.Reboot(this.Connection);
            }

        }).ContinueWith(result =>
        {
            parentViewModel.FinishProcess("Reboot Display Sent", !result.IsFaulted);
        });
    }

    internal void StopApplication()
    {
        parentViewModel.Console.ClearConsole();

        parentViewModel.StartProcess(null);

        IsConnected = false;
        Task.Run(() =>
        {
            var prep = RemoteTargetToolFactory.GetToolsForPlatform(this.Connection.PlatformFamily);
            prep.StopApplication(this.Connection, IsInstaller);

        }).ContinueWith(result =>
        {
            parentViewModel.FinishProcess("Stop Application Sent", !result.IsFaulted);
        });
    }

    public void ShowConnectionDetails()
    {
        if (parentViewModel.Popup.RootPopup is ConnectionDetailsView)
            parentViewModel.Popup.DismissPopup();
        else
            parentViewModel.Popup.ShowPopup(new ConnectionDetailsView() { DataContext = this });
    }

    public void ClosePopup()
    {
        parentViewModel.Popup.DismissPopup();
    }

    internal GetHardwareInformationReply ReturnToInstaller(CancellationTokenSource source, IProgress<string> progressString)
    {
        lock (syncRoot)
        {
            // Restart the Installer
            TargetConnectionInfo connectionInfo = new()
            {
                HostName = parentViewModel.ActiveConnection.Connection.HostName,
                UserName = parentViewModel.ActiveConnection.Connection.UserName,
                PrivateKeyFile = parentViewModel.ActiveConnection.Connection.PrivateKeyFile,
            };

            // Switch to Installer and Check Result.
            var prep = RemoteTargetToolFactory.GetToolsForPlatform(connectionInfo.PlatformFamily);
            return prep.ReturnToInstaller(connectionInfo, client, source, progressString);
        }
    }
    #endregion

}


