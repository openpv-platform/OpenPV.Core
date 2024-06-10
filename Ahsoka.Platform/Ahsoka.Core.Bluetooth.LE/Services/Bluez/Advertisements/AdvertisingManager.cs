using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Advertisements;

internal class AdvertisingManager
{
    private readonly Connection connection;
    private readonly string adapterPath;

    public AdvertisingManager(Connection connection, string bluezAdapterPath)
    {
        this.connection = connection;
        adapterPath = bluezAdapterPath;
    }

    private ILEAdvertisingManager1 GetAdvertisingManager()
    {
        return connection.CreateProxy<ILEAdvertisingManager1>("org.bluez", adapterPath);
    }

    public async Task CreateAdvertisement(string localName, BLEAdvertisementProperties advertisementProperties)
    {
        var advertisement = new Advertisement($"{adapterPath}/{localName}", advertisementProperties);
        await RegisterAdvertisement(advertisement);
    }

    public async Task RemoveAdvertisement(string localName)
    {
        var advertisement = $"{adapterPath}/{localName}";
        await UnRegisterAdvertisement(advertisement);
    }

    private async Task RegisterAdvertisement(Advertisement advertisement)
    {
        await connection.RegisterObjectAsync(advertisement);
        await GetAdvertisingManager().RegisterAdvertisementAsync(((IDBusObject)advertisement).ObjectPath,
            new Dictionary<string, object>());
    }

    private async Task UnRegisterAdvertisement(string advertObjectPath)
    {
        await GetAdvertisingManager().UnregisterAdvertisementAsync(advertObjectPath);
    }


}