using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Services.Security;
using Ahsoka.Services.System;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Timer = System.Timers.Timer;

namespace Ahsoka.DeveloperTools;

internal partial class ConnectionInfo : ChildViewModelBase<ConnectionsViewModel>
{
    #region Fields
    SystemServiceClient client;
    SecurityServiceClient securityClient;
    readonly object syncRoot = new();

    GetHardwareInformationReply hardwareInfo;
    bool needsReconnect = false;
    bool isActive = false;
    bool isConnected = false;
    bool isInited = false;
    bool isLoading = false;

    readonly Timer timer = new(5000);
    #endregion

    #region Properties

    public bool IsActive 
    {
        get { return isActive; }
        internal set 
        { 
            isActive = value; 

            UpdateConnectedDescription();
            
            if (!isInited)
                Init();

            if (IsActive)
                timer.Start();
            else
                timer.Stop();
        } 
    }

    public bool IsConnected
    {
        get
        {
            return isConnected;
        }
        set
        {
            isConnected = value;
            UpdateConnectedDescription();
        }
    }
    #endregion

    public void Init()
    {
        // Create Connection
        lock (syncRoot)
            StartConnection();

        // Start Timer to Listen
        timer.Elapsed += Timer_Tick;

        needsReconnect = false;
        isInited = true;
    }

    private void StartConnection()
    {
        var config = new ServiceConfiguration()
        {
            ServiceName = "SystemService",
            TcpConnectionAddress = HostName,
            DataChannel = SystemServicePort,
        };
        var securityConfig = new ServiceConfiguration()
        {
            ServiceName = "SecurityService",
            TcpConnectionAddress = HostName,
            DataChannel = SecurityServicePort,
        };

        if (client == null)
        {
            client = new(config);
            securityClient = new(securityConfig);

            UpdateConnectedDescription();
            client.Start();
        }
        else
        {
            UpdateConnectedDescription();
            client.Stop();

            if (securityClient.IsStarted)
                securityClient.Stop();

            client = new(config);
            securityClient = new(securityConfig);

            client.Start();
        }
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        try
        {
            timer.Stop();

            // Skip if loading
            if (!isLoading)
            {
                lock (syncRoot)
                    AttemptGetHardware();
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("InvalidToken"))
            {
                var request = securityClient.RequestToken(new TokenRequestInfo()
                {
                    ApiKey = APIKey,
                    ClientIds = { ((IAhsokaClientEndPoint)client).GetClientId() },
                    ClientDescription = "OpenPV Desktop"
                });

                // Waits for Accepted Token to be Recieved
                var systemServiceToken = securityClient.WaitForToken(request.RequestId);
                if (systemServiceToken == null || !systemServiceToken.TokenIsValid)
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, "The token was not valid");
                else
                    client.SetToken(systemServiceToken);

                lock (syncRoot)
                    AttemptGetHardware();
            }
            else
            {
                IsConnected = false;
            }
        }
        finally
        {
            timer.Start();
        }
    }

    private bool AttemptGetHardware()
    {
        // Start Connection Again if Address Changed
        if (needsReconnect)
            StartConnection();

        var newInfo = client.GetHardwareInformation(2000);

        if (this.IsActive)
        {
            // Clear any Line Endings
            if (newInfo != null)
            {
                var items = newInfo.VersionInfo.ToArray();
                newInfo.VersionInfo.Clear();
                foreach (var item in items)
                    newInfo.VersionInfo[item.Key] = item.Value.ReplaceLineEndings("");
            }

            IsConnected = ParentViewModel.ParentViewModel.ActiveConnection.IsConnected 
                        = newInfo != null;
            hardwareInfo = ParentViewModel.ParentViewModel.ActiveConnection.ConnectedHardwareInfo 
                         = newInfo;
            ParentViewModel.ParentViewModel.ActiveConnection.IsInstaller = newInfo != null && newInfo.IsInstaller;

            UpdateConnectedDescription();
        }

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
        {
            isLoading = true;
            try
            {
                return client.LoadApplicationAsync(stream, autoLoad, progressString);
            }
            finally { isLoading = false; }
        }
    }

    private void UpdateConnectedDescription()
    {
        if (this.IsActive)
        {
            string isConnectedString = IsConnected ? "Connected" : "Waiting for Connection";
            string appName = hardwareInfo != null ? $"({hardwareInfo?.ApplicationName})" : "";

            ParentViewModel.ParentViewModel.ActiveConnection.IsConnected = this.IsConnected;
            ParentViewModel.ParentViewModel.ActiveConnection.ConnectionDescription = $"{isConnectedString} to {(string.IsNullOrEmpty(Description) ? ConnectionAddress : Description)} {appName}";
        }
    }

    internal void CancelLoadApplication()
    {
        client.CancelLoadApplication();
    }

    internal void RebootDisplay()
    {
        ParentViewModel.ParentViewModel.Console.ClearConsole();
        ParentViewModel.ParentViewModel.StartProcess();
        
        IsConnected = false;
        Task.Run(() =>
        {
            var prep = RemoteTargetToolFactory.GetToolsForPlatform(this.PlatformFamily);
            prep.Reboot(this);

        }).ContinueWith(result =>
        {
            ParentViewModel.ParentViewModel.FinishProcess("Reboot Display Sent", !result.IsFaulted);
        });
    }

    internal void StopApplication()
    {
        ParentViewModel.ParentViewModel.Console.ClearConsole();

        ParentViewModel.ParentViewModel.StartProcess(null);

        IsConnected = false;
        Task.Run(() =>
        {
            var prep = RemoteTargetToolFactory.GetToolsForPlatform(this.PlatformFamily);
            prep.StopApplication(this, ParentViewModel.ParentViewModel.ActiveConnection.IsInstaller);

        }).ContinueWith(result =>
        {
            ParentViewModel.ParentViewModel.FinishProcess("Stop Application Sent", !result.IsFaulted);
        });
    }

    internal GetHardwareInformationReply ReturnToInstaller(CancellationTokenSource source, IProgress<string> progressString)
    {
        // Restart the Installer
        TargetConnectionInfo connectionInfo = new()
        {
            HostName = ParentViewModel.ParentViewModel.ActiveConnection.Connection.HostName,
            UserName = ParentViewModel.ParentViewModel.ActiveConnection.Connection.UserName,
            PrivateKeyFile = ParentViewModel.ParentViewModel.ActiveConnection.Connection.PrivateKeyFile,
        };

        // Switch to Installer and Check Result.
        var prep = RemoteTargetToolFactory.GetToolsForPlatform(connectionInfo.PlatformFamily);
        return prep.ReturnToInstaller(connectionInfo, client, source, progressString);
    }
}
