using Ahsoka.Core;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network.Cellular.Implementation;

[ExcludeFromCodeCoverage]
internal class DesktopImplementation : ImplementationBase
{
    static string adapterId = "/dev/unknown-adapter";
    CellularNetworkInformation info = new CellularNetworkInformation() { AdapterId = adapterId, ConnectionState = CellularNetworkState.NotConnected, NetworkOperatorName = "Unknown", ServiceName = "Unknown" };

    protected override void OnInitNetwork(out CellularServiceConfiguration defaultConfiguration, out AdapterRequest defaultRequest)
    {
        // No Configuration Needed on Windows
        defaultConfiguration = new CellularServiceConfiguration() { AdapterId = adapterId, };
        defaultRequest = new AdapterRequest() { AdapterId = adapterId };
    }

    protected override CellularConnectionResult OnStartNetwork(AhsokaServiceBase<CellularMessageTypes.Ids>.AhsokaServiceRequest messageHeader, CellularServiceConfiguration connectionRequest)
    {
        info.ConnectionState = CellularNetworkState.ConnectionInProgress;
        NetworkService.SendConnectionStateNotification(messageHeader, info);

        info.ConnectionState = CellularNetworkState.Connected;
        NetworkService.SendConnectionStateNotification(messageHeader, info);

        return new CellularConnectionResult() { AdapterId = adapterId, ConnectionStarted = true };
    }

    protected override void OnStopNetwork(AhsokaServiceBase<CellularMessageTypes.Ids>.AhsokaServiceRequest messageHeader, string adapterId)
    {
        info.ConnectionState = CellularNetworkState.NotConnected;
        NetworkService.SendConnectionStateNotification(messageHeader, info);
    }

    internal override CellularNetworkInformation GetNetworkInfo(AdapterRequest cellularAdapterInfo)
    {
        return info;
    }
}
