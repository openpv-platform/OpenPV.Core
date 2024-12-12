using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network.ESPWifi;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ahsoka.Services.Network.Platform.ESPWifi;

internal static class InteropList
{
    static readonly Dictionary<string, InteropData> interfaceList = new();

    static internal void Add(string adapterId)
    {
        interfaceList[adapterId] = new(adapterId);
    }

    static internal InteropLock AquireInterop(string adapterId)
    {
        var connection = interfaceList[adapterId];
        if (connection != null)
        {
            var interopLock = connection.CreateLock();
            interopLock.Lock();
            return interopLock;
        }

        return null;
    }
}


internal class InteropData
{
    internal ESPInterop ESPInterop { get; set; }
    protected Mutex Mutex { get; set; }
    protected string AdapterID { get; set; }

    protected InteropData() { }

    internal InteropData(string adapterId)
    {
        ESPInterop = new ESPInterop();
        AdapterID = adapterId;
        Mutex = new Mutex(false, $"ESPMutex_{AdapterID}");
    }

    public InteropLock CreateLock()
    {
        return new InteropLock()
        {
            AdapterID = AdapterID,
            ESPInterop = ESPInterop,
            Mutex = Mutex
        };
    }
}

internal class InteropLock : InteropData, IDisposable
{
    internal void Lock()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Acquiring Lock on {AdapterID}");
        Mutex.WaitOne();
    }

    void IDisposable.Dispose()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Releasing Lock on {AdapterID}");
        Mutex.ReleaseMutex();
    }

}