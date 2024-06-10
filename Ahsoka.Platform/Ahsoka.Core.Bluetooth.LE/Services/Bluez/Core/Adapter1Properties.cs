using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Core;

[Dictionary]
class Adapter1Properties
{
    private string _Address = default;
    public string Address
    {
        get
        {
            return _Address;
        }

        set
        {
            _Address = value;
        }
    }

    private string _AddressType = default;
    public string AddressType
    {
        get
        {
            return _AddressType;
        }

        set
        {
            _AddressType = value;
        }
    }

    private string _Name = default;
    public string Name
    {
        get
        {
            return _Name;
        }

        set
        {
            _Name = value;
        }
    }

    private string _Alias = default;
    public string Alias
    {
        get
        {
            return _Alias;
        }

        set
        {
            _Alias = value;
        }
    }

    private uint _Class = default;
    public uint Class
    {
        get
        {
            return _Class;
        }

        set
        {
            _Class = value;
        }
    }

    private bool _Powered = default;
    public bool Powered
    {
        get
        {
            return _Powered;
        }

        set
        {
            _Powered = value;
        }
    }

    private bool _Discoverable = default;
    public bool Discoverable
    {
        get
        {
            return _Discoverable;
        }

        set
        {
            _Discoverable = value;
        }
    }

    private uint _DiscoverableTimeout = default;
    public uint DiscoverableTimeout
    {
        get
        {
            return _DiscoverableTimeout;
        }

        set
        {
            _DiscoverableTimeout = value;
        }
    }

    private bool _Pairable = default;
    public bool Pairable
    {
        get
        {
            return _Pairable;
        }

        set
        {
            _Pairable = value;
        }
    }

    private uint _PairableTimeout = default;
    public uint PairableTimeout
    {
        get
        {
            return _PairableTimeout;
        }

        set
        {
            _PairableTimeout = value;
        }
    }

    private bool _Discovering = default;
    public bool Discovering
    {
        get
        {
            return _Discovering;
        }

        set
        {
            _Discovering = value;
        }
    }

    private string[] _UUIDs = default;
    public string[] UUIDs
    {
        get
        {
            return _UUIDs;
        }

        set
        {
            _UUIDs = value;
        }
    }

    private string _Modalias = default;
    public string Modalias
    {
        get
        {
            return _Modalias;
        }

        set
        {
            _Modalias = value;
        }
    }
}
