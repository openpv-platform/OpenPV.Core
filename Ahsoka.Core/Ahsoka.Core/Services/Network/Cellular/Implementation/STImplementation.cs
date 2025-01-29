using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Network.Cellular.Implementation;

[ExcludeFromCodeCoverage]
internal class STImplementation : ImplementationBase
{
    // We currently only support a single wwan0 interface
    static string adapterName = "wwan0";
    static string adapterId = "/dev/cdc-wdm0";

    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    CellularServiceConfiguration currentConfig = new() { AdapterId = adapterId };
    CellularNetworkInformation info = new CellularNetworkInformation() { AdapterId = adapterId, ConnectionState = CellularNetworkState.NotConnected, NetworkOperatorName = "Unknown", ServiceName = "Unknown" };
    Task connectionTask = null;

    protected override void OnInitNetwork(out CellularServiceConfiguration defaultConfiguration, out AdapterRequest defaultRequest)
    {
        // No Configuration Needed on Windows
        defaultConfiguration = currentConfig;
        defaultRequest = new AdapterRequest() { AdapterId = adapterId };
    }

    internal override CellularNetworkInformation GetNetworkInfo(AdapterRequest cellularAdapterInfo)
    {
        return info;
    }

    protected override CellularConnectionResult OnStartNetwork(AhsokaServiceBase<CellularMessageTypes.Ids>.AhsokaServiceRequest messageHeader, CellularServiceConfiguration connectionRequest)
    {
        if (connectionTask != null)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Ignoring Start Request as Existing Connection is Running - Call Stop Network to Start a New Network Setup");
            return new() { AdapterId = adapterId, ConnectionStarted = false, Description = "Existing Connection in Progress" };
        }

        bool adapterReadyResult = Path.Exists(adapterId);
        if (adapterReadyResult)
            connectionTask = Task.Run(() =>
            {
                currentConfig = connectionRequest;

                bool connectResult = false;
                info = new CellularNetworkInformation() { AdapterId = adapterId, ConnectionState = CellularNetworkState.ConnectionInProgress, IpInfo = new() };

                NetworkService.SendConnectionStateNotification(messageHeader, info);

                // Write Connection Configuration to /etc/
                string config = $"APN={connectionRequest.Apn}\nAPN_USER={connectionRequest.ApnUser}\nAPN_PASS={connectionRequest.ApnPassword}\nPROXY=no\n";
                File.WriteAllText("/etc/qmi-network.conf", config);

                string command = $"ip link set {adapterName} down;echo 'Y' | tee /sys/class/net/{adapterName}/qmi/raw_ip;ip link set {adapterName} up";
                ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out string log, out string error);

                // Check Link Result
                connectResult = log.StartsWith("Y") && error == "";
                if (!connectResult)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler Link Log: {log} {error}");
                    return;
                }

                // Manual Setup if needed.  We use a Config File and use network "start" instead.
                //qmicli -p -d /dev/cdc-wdm0 --device-open-net='net-raw-ip|net-no-qos-header' --wds-start-network="apn='AP',username='UserName',password='PassWord',ip-type=4" --client-no-release-cid

                command = $"qmi-network {adapterId} start";
                ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out log, out error);

                // Check Network Start Results
                connectResult = (log.Contains("Network started successfully") && error == "") || error.Contains("error: cannot re-start network");
                if (!connectResult)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler Network Start: {log} {error}");
                    return;
                }

                command = $"udhcpc -q -f -i {adapterName}";
                ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out log, out error);

                connectResult = log.Contains("udhcpc: lease of") || error.Contains("udhcpc: lease of");
                if (!connectResult)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler DHCP Start: {log} {error}");
                    return;
                }

                // Determine IP Address - if Not set, error out.
                info.IpInfo = Client.Implementation.ImplementationBase.GetIPInfo(new AdapterRequest() { AdapterId = adapterName });
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler DHCP Address: {info.IpInfo.IPAddress}");

                // Execute Time Sync
                bool resultTime = LinuxNetworkUtilities.WaitForTimeSync(connectionRequest.WaitForTimeSyncSeconds > 0 ? connectionRequest.WaitForTimeSyncSeconds : 10);
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler Time Sync Completed: {resultTime}");

                info.ConnectionState = CellularNetworkState.Connected;
                NetworkService.SendConnectionStateNotification(messageHeader, info);

                try
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested && info.ConnectionState == CellularNetworkState.Connected)
                    {
                        Task.Delay(5000).Wait(cancellationTokenSource.Token);

                        string statusCommand = $"qmicli -d {adapterId} --nas-get-operator-name ; qmicli -d {adapterId} --nas-get-signal-info ; qmi-network {adapterId} status";
                        ProcessUtility.RunProcess("/bin/sh", $"-c \"{statusCommand}", null, out log, out error, 2000);

                        bool changed = ProcessStatus(log);
                        if (changed)
                            NetworkService.SendConnectionStateNotification(messageHeader, info);
                    }
                }
                catch (OperationCanceledException) { }

            });

        return new() { AdapterId = adapterId, ConnectionStarted = adapterReadyResult, Description = "Connection Started Async - Check Network State Notifications for More Information" };
    }

    private bool ProcessStatus(string log)
    {
        bool result = false;
        foreach (var item in log.Split("\n"))
        {
            string name = TryParseValue(item, "Short Name");
            if (name != null && name != info.NetworkOperatorName)
            {
                info.NetworkOperatorName = name;
                result = true;
                continue;
            }

            name = TryParseValue(item, "Name");
            if (name != null && name != info.ServiceName)
            {
                info.ServiceName = name;
                result = true;
                continue;
            }

            name = TryParseValue(item, "RSSI");
            if (name != null && name != info.Rssi)
            {
                info.Rssi = name;
                result = true;
                continue;
            }

            name = TryParseValue(item, "RSRQ");
            if (name != null && name != info.Rsrq)
            {
                info.Rsrq = name;
                result = true;
                continue;
            }

            name = TryParseValue(item, "RSRP");
            if (name != null && name != info.Rsrp)
            {
                info.Rsrp = name;
                result = true;
                continue;
            }

            name = TryParseValue(item, "SNR");
            if (name != null && name != info.Snr)
            {
                info.Snr = name;
                result = true;
                continue;

            }

            name = TryParseValue(item, "Status");
            if (name != null && !name.Contains("connected"))
            {
                info.ConnectionState = CellularNetworkState.NotConnected;
                result = true;
            }
        }

        return result;

        /*[/dev/cdc-wdm0] Successfully got operator name data
          Service Provider Name
                  Display Condition: 'none'
                  Name             : 'Hologram'
          NITZ information:
                  Long Name:  'AT&T'
                  Short Name: 'AT&T'
                  Country:    'initials-do-not-add'
          [/dev/cdc-wdm0] Successfully got signal info
          LTE:
                  RSSI: '-77 dBm'
                  RSRQ: '-13 dB'
                  RSRP: '-109 dBm'
                  SNR: '8.8 dB'
          Loading profile at /etc/qmi-network.conf...
              APN: hologram
              APN user: unset
              APN password: unset
              qmi-proxy: no
              IP_TYPE: unset
              PROFILE: unset
          Loading previous state from /tmp/qmi-network-state-cdc-wdm0...
              Previous CID: 3
              Previous PDH: 2248811088
          Getting status with 'qmicli -d /dev/cdc-wdm0 --wds-get-packet-service-status --client-cid=3 --client-no-release-cid '...
          Status: connected*/

    }

    private string TryParseValue(string line, string tag)
    {
        try
        {
            if (line.Trim().StartsWith(tag))
                return line.Substring(line.IndexOf(":")).Trim().Replace("'", "");
        }
        catch { }

        return null;
    }

    protected override void OnStopNetwork(AhsokaServiceBase<CellularMessageTypes.Ids>.AhsokaServiceRequest messageHeader, string adapterId)
    {
        try
        {
            // Shutdown Network Status
            cancellationTokenSource.Cancel();
            connectionTask.Wait(2500);
            connectionTask = null;
        }
        catch { }

        string command = $"qmi-network {adapterId} stop";
        ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out string log, out string error);

        // Check Network Start Results
        bool connectResult = (log.Contains("Network stopped successfully") && error == "") || error.Contains("error: cannot re-start network");
        if (!connectResult)
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler Network Stop: {log} {error}");

        File.Delete("/etc/qmi-network.conf");

        command = $"ip link set {adapterName} down";
        ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out log, out error);

        NetworkService.SendConnectionStateNotification(messageHeader, new CellularNetworkInformation() { IpInfo = new(), AdapterId = adapterId, ConnectionState = CellularNetworkState.NotConnected, NetworkOperatorName = "Unknown", ServiceName = "Unknown" });
    }
}
