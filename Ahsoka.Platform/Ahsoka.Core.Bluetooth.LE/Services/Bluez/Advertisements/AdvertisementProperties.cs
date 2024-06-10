using System.Collections.Generic;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements;

[Dictionary]
internal class BLEAdvertisementProperties
{
    private string _Type;
    public string Type
    {
        get => _Type;
        set => _Type = value;
    }

    private string[] _Includes;
    public string[] Includes
    {
        get => _Includes;
        set => _Includes = value;
    }

    private string[] _ServiceUUIDs;
    public string[] ServiceUUIDs
    {
        get => _ServiceUUIDs;
        set => _ServiceUUIDs = value;
    }

    private IDictionary<ushort, object> _ManufacturerData;
    public IDictionary<ushort, object> ManufacturerData
    {
        get => _ManufacturerData;
        set => _ManufacturerData = value;
    }

    private string[] _SolicitUUIDs;
    public string[] SolicitUUIDs
    {
        get => _SolicitUUIDs;
        set => _SolicitUUIDs = value;
    }

    private IDictionary<string, object> _ServiceData;
    public IDictionary<string, object> ServiceData
    {
        get => _ServiceData;
        set => _ServiceData = value;
    }
}