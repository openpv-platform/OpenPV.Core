using Ahsoka.ServiceFramework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Network.AP.Implementation;

internal abstract class ImplementationBase
{
    #region Fields
    object syncRoot = new object();
    AccessPointServiceConfiguration networkConfiguration = null;
    AdapterRequest networkAdapter = null;
    Task notificationTask;
    AccessPointClientList connectedList = new();
    CancellationTokenSource cancellationTokenSource;
    #endregion

    #region Properties
    public WiFiAPService NetworkService { get; private set; }
    #endregion

    #region Methods
    public ImplementationBase()
    {

    }

    public void Init(WiFiAPService service)
    {
        this.NetworkService = service;
    }


    internal AdapterRequestList GetAdapters()
    {
        if (networkConfiguration == null)
        {
            // Init
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Starting Wifi Client Networking...");

            // TODO: Find Network Adapter for Service
            OnInit(out networkConfiguration, out networkAdapter);
        }

        return new AdapterRequestList()
        {
            Adapters = { new AdapterRequest() { AdapterId = networkAdapter?.AdapterId } }
        };
    }

    internal AccessPointClientList GetClientList(AdapterRequest adapterRequest)
    {
        lock (syncRoot)
            return connectedList;
    }

    internal AccessPointServiceConfiguration GetAdapterConfiguration(AdapterRequest adapterRequest)
    {
        return networkConfiguration;
    }

    internal AdapterResult StartAccessPoint(AdapterRequest adapterRequest)
    {
        var result = OnStartAccessPoint(adapterRequest);

        notificationTask = Task.Run(() =>
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    lock (syncRoot)
                    {
                        var newList = GetClientList(adapterRequest);
                        foreach (var item in newList.Clients)
                            if (!connectedList.Clients.Any(x => x.MacAddress == item.MacAddress))
                                NetworkService.SendNotification(WiFiAPMessageTypes.Ids.ClientAdded, item);

                        foreach (var item in connectedList.Clients.ToArray())
                            if (!newList.Clients.Any(x => x.MacAddress == item.MacAddress))
                                NetworkService.SendNotification(WiFiAPMessageTypes.Ids.ClientRemoved, item);

                        connectedList = newList;
                    }

                    Task.Delay(1000, cancellationTokenSource.Token).Wait();
                }

            }
            catch (OperationCanceledException) { } // Handle Token Cancellation
        });

        return result;
    }

    internal AdapterResult StopAccessPoint(AdapterRequest adapterRequest)
    {
        cancellationTokenSource?.Cancel();
        notificationTask?.Wait(5000);
        connectedList.Clients.Clear();
        return OnStopAccessPoint(adapterRequest);
    }

    internal AdapterResult UpdateConfiguration(AccessPointServiceConfiguration adapterConfiguration)
    {
        var result = OnValidateConfiguration(adapterConfiguration);

        if (!result.ConfigurationSuccessful)
            return result;

        networkConfiguration = adapterConfiguration;

        OnUpdateConfiguration(adapterConfiguration);

        return result;
    }

    protected virtual AdapterResult OnValidateConfiguration(AccessPointServiceConfiguration adapterConfiguration)
    {
        AdapterResult result = new() { AdapterId = adapterConfiguration.AdapterId, ConfigurationSuccessful = true };
        return result;
    }
    #endregion

    #region Abstract Calls
    protected abstract void OnInit(out AccessPointServiceConfiguration defaultConfiguration, out AdapterRequest defaultRequest);

    protected abstract void OnUpdateConfiguration(AccessPointServiceConfiguration adapterConfiguration);

    protected abstract AdapterResult OnStartAccessPoint(AdapterRequest adapterRequest);

    protected abstract AdapterResult OnStopAccessPoint(AdapterRequest adapterRequest);

    protected abstract AccessPointClientList OnGetClientList(AdapterRequest adapterRequest);
    #endregion
}
