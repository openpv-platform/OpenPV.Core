#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network.Cellular.Implementation;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network;

[AhsokaService(Name)]
public class CellularService : AhsokaServiceBase<CellularMessageTypes.Ids>
{
    #region Fields
    readonly ImplementationBase networkInterface;
    public const string Name = "CellularService";
    #endregion

    #region Methods
    internal static ImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new DesktopImplementation(),
            PlatformFamily.OpenViewLinux => new STImplementation(),
            PlatformFamily.OpenViewLinuxPro => new DesktopImplementation(),
            PlatformFamily.Ubuntu64 => new DesktopImplementation(),
            PlatformFamily.MacosArm64 => new DesktopImplementation(),
            _ => throw new NotImplementedException(),
        };
    }

    public CellularService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public CellularService(ServiceConfiguration config) :
        base(config, new CellularServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create CellularService");

        networkInterface = GetInterop();
        networkInterface.Init(this);

        stopwatch.StopTiming("Cellular Init Completed");

        UpdateCacheValue(CellularServiceMessages.Available, true);
    }

    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case CellularMessageTypes.Ids.GetAdapters:
                GetAdapterRequest(request);
                break;

            case CellularMessageTypes.Ids.GetNetworkInformation:
                GetNetworkInformation(request, request.Message as AdapterRequest);
                break;

            case CellularMessageTypes.Ids.StartNetwork:
                StartNetwork(request, request.Message as CellularServiceConfiguration);
                break;

            case CellularMessageTypes.Ids.StopNetwork:
                StopNetwork(request, request.Message as AdapterRequest);
                break;
        }
    }


    private void GetAdapterRequest(AhsokaServiceRequest request)
    {
        // Send Empty Response to Ack.
        var result = networkInterface.GetAdapters();
        SendResponse(request, result);
    }

    private void GetNetworkInformation(AhsokaServiceRequest request, AdapterRequest cellularAdapterInfo)
    {
        CellularNetworkInformation info = networkInterface.GetNetworkInfo(cellularAdapterInfo);
        SendResponse(request, info);
    }

    [ExcludeFromCodeCoverage]
    private void StartNetwork(AhsokaServiceRequest messageHeader, CellularServiceConfiguration connectionRequest)
    {
        CellularConnectionResult result = networkInterface.StartNetwork(messageHeader, connectionRequest);
        SendResponse(messageHeader, result);
    }

    [ExcludeFromCodeCoverage]
    private void StopNetwork(AhsokaServiceRequest messageHeader, AdapterRequest request)
    {
        networkInterface.StopNetwork(messageHeader, request.AdapterId);
        SendResponse(messageHeader);
    }

    [ExcludeFromCodeCoverage]
    internal void SendConnectionStateNotification(AhsokaServiceRequest messageHeader, CellularNetworkInformation connectionResult)
    {
        SendNotification(CellularMessageTypes.Ids.NetworkStateChanged, connectionResult);
    }
    #endregion
}
