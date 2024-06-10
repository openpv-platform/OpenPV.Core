using Ahsoka.Services.Bluetooth.LE.Bluez.Core;
using System.Threading.Tasks;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements;

internal class Advertisement : PropertiesBase<BLEAdvertisementProperties>, ILEAdvertisement1
{
    public Advertisement(string objectPath, BLEAdvertisementProperties properties) :
        base(objectPath, properties)
    {
    }

    public Task ReleaseAsync()
    {
        return Task.CompletedTask;
    }
}