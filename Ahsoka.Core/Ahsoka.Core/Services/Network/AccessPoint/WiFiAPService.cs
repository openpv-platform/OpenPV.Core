#pragma warning disable CS1591
using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Ahsoka.Services.Network.AP.Implementation;
using System;

namespace Ahsoka.Services.Network;

[AhsokaService(Name)]
public class WiFiAPService : AhsokaServiceBase<WiFiAPMessageTypes.Ids>
{
    #region Fields
    readonly ImplementationBase networkInterface;
    public const string Name = "WiFiAPService";
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
            PlatformFamily.MacOSArm64 => new DesktopImplementation(),
            _ => throw new NotImplementedException(),
        };
    }

    public WiFiAPService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public WiFiAPService(ServiceConfiguration config) :
        base(config, new WiFiAPServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create NetworkService");

        networkInterface = GetInterop();
        stopwatch.ResetTiming("Interop Loaded");

        networkInterface.Init(this);
        stopwatch.StopTiming("Network Init Completed");

        UpdateCacheValue(WiFiClientServiceMessages.Available, true);
    }

    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case WiFiAPMessageTypes.Ids.GetAdapters:
                GetAdapterRequest(request);
                break;

            case WiFiAPMessageTypes.Ids.StartAccessPoint:
                StartAccessPoint(request, request.Message as AdapterRequest);
                break;

            case WiFiAPMessageTypes.Ids.StopAccessPoint:
                StopAccessPoint(request, request.Message as AdapterRequest);
                break;

            case WiFiAPMessageTypes.Ids.GetNetworkConfiguration:
                GetNetworkConfiguration(request, request.Message as AdapterRequest);
                break;

            case WiFiAPMessageTypes.Ids.SetNetworkConfiguration:
                SetNetworkConfiguration(request, request.Message as AccessPointServiceConfiguration);
                break;

            case WiFiAPMessageTypes.Ids.GetClientList:
                GetClientList(request, request.Message as AdapterRequest);
                break;
        }
    }

    private void GetClientList(AhsokaServiceRequest request, AdapterRequest adapterRequest)
    {
        // Send Empty Response to Ack.
        var result = networkInterface.GetClientList(adapterRequest);
        SendResponse(request, result);
    }

    private void GetAdapterRequest(AhsokaServiceRequest request)
    {
        // Send Empty Response to Ack.
        var result = networkInterface.GetAdapters();
        SendResponse(request, result);
    }

    private void StartAccessPoint(AhsokaServiceRequest request, AdapterRequest adapterRequest)
    {
        // Send Empty Response to Ack.
        var result = networkInterface.StartAccessPoint(adapterRequest);
        SendResponse(request, result);
    }

    private void StopAccessPoint(AhsokaServiceRequest request, AdapterRequest adapterRequest)
    {
        // Send Empty Response to Ack.
        var result = networkInterface.StopAccessPoint(adapterRequest);
        SendResponse(request, result);
    }

    private void GetNetworkConfiguration(AhsokaServiceRequest messageHeader, AdapterRequest request)
    {
        var response = networkInterface.GetAdapterConfiguration(request);
        SendResponse(messageHeader, response);
    }

    private void SetNetworkConfiguration(AhsokaServiceRequest messageHeader, AccessPointServiceConfiguration adapterConfiguration)
    {
        var result = networkInterface.UpdateConfiguration(adapterConfiguration);
        SendResponse(messageHeader, result);
    }

    #endregion
}
