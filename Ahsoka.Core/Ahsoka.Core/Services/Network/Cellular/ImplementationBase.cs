using Ahsoka.ServiceFramework;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Ahsoka.Services.Network.Cellular.Implementation;

internal abstract class ImplementationBase
{
    #region Fields
    CellularServiceConfiguration networkConfiguration = null;
    CellularNetworkInformation networkInformation = new();
    AdapterRequest networkAdapter;
    #endregion

    #region Properties
    public CellularService NetworkService { get; private set; }
    #endregion

    #region Methods
    public ImplementationBase()
    {

    }

    public void Init(CellularService service)
    {
        this.NetworkService = service;
    }


    internal AdapterRequestList GetAdapters()
    {
        if (networkConfiguration == null)
        {
            // Init
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Starting Cellular Networking...");
            OnInitNetwork(out networkConfiguration, out networkAdapter);
        }

        return new AdapterRequestList()
        {
            Adapters = { new AdapterRequest() { AdapterId = networkAdapter?.AdapterId } }
        };
    }

    internal CellularConnectionResult StartNetwork(CellularService.AhsokaServiceRequest messageHeader, CellularServiceConfiguration connectionRequest)
    {
        return OnStartNetwork(messageHeader, connectionRequest);
    }

    internal void StopNetwork(CellularService.AhsokaServiceRequest messageHeader, string adapterId)
    {
        OnStopNetwork(messageHeader, adapterId);
    }

    internal IPInfo GetIPInfo(AdapterRequest adapterRequest)
    {
        IPInfo ipInfo = new();
        foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netif.Id.ToLower().Contains(adapterRequest.AdapterId.ToLower())
                && netif.OperationalStatus == OperationalStatus.Up)
            {
                ipInfo.AdapterId = netif.Id.ToString();
                ipInfo.MacAddress = netif.GetPhysicalAddress().ToString();

                IPInterfaceProperties properties = netif.GetIPProperties();

                foreach (var ipAddress in properties.DnsAddresses)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                        ipInfo.Dns.Add(ipAddress.ToString());
                }

                foreach (IPAddressInformation ipAddress in properties.UnicastAddresses)
                {
                    if (ipAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        ipInfo.IPAddress = ipAddress.Address.ToString();
                }
            }

        }

        // Update IPAddress 
        this.NetworkService.UpdateCacheValue(WiFiClientServiceMessages.IPAddress, ipInfo.IPAddress);

        return ipInfo;
    }

    #endregion

    #region Abstract Calls
    protected abstract void OnInitNetwork(out CellularServiceConfiguration defaultConfiguration, out AdapterRequest defaultRequest);

    protected abstract CellularConnectionResult OnStartNetwork(CellularService.AhsokaServiceRequest messageHeader, CellularServiceConfiguration connectionRequest);

    protected abstract void OnStopNetwork(CellularService.AhsokaServiceRequest messageHeader, string adapterId);

    internal abstract CellularNetworkInformation GetNetworkInfo(AdapterRequest cellularAdapterInfo);

    #endregion
}
