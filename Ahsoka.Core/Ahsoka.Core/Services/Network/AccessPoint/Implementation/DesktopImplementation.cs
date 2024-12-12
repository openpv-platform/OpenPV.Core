using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Ahsoka.Services.Network.Platform;

namespace Ahsoka.Services.Network.AP.Implementation;

[ExcludeFromCodeCoverage]
internal class DesktopImplementation : ImplementationBase
{
    readonly CancellationTokenSource tokenSource = new();
    readonly AutoResetEvent disconnectListenerCompleted = new(false);
    string adapterId = Guid.NewGuid().ToString();

    protected override void OnInit(out AccessPointServiceConfiguration defaultConfiguration, out AdapterRequest defaultRequest)
    {
        // No Configuration Needed on Windows
        defaultConfiguration = new AccessPointServiceConfiguration() { AdapterId = adapterId };
        defaultRequest = new AdapterRequest() { AdapterId = adapterId };
    }

    protected override AdapterResult OnStartAccessPoint(AdapterRequest adapterRequest)
    {
        return new AdapterResult();
    }

    protected override AdapterResult OnStopAccessPoint(AdapterRequest adapterRequest)
    {
        return new AdapterResult();
    }

    protected override void OnUpdateConfiguration(AccessPointServiceConfiguration adapterConfiguration)
    {

    }

    protected override AccessPointClientList OnGetClientList(AdapterRequest adapterRequest)
    {
        return new AccessPointClientList();
    }
}
