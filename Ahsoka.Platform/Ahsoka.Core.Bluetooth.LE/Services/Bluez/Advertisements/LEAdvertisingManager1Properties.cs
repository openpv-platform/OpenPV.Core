using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements;

[Dictionary]
class LEAdvertisingManager1Properties
{
    private byte _ActiveInstances = default;
    public byte ActiveInstances
    {
        get
        {
            return _ActiveInstances;
        }

        set
        {
            _ActiveInstances = value;
        }
    }

    private byte _SupportedInstances = default;
    public byte SupportedInstances
    {
        get
        {
            return _SupportedInstances;
        }

        set
        {
            _SupportedInstances = value;
        }
    }

    private string[] _SupportedIncludes = default;
    public string[] SupportedIncludes
    {
        get
        {
            return _SupportedIncludes;
        }

        set
        {
            _SupportedIncludes = value;
        }
    }
}
