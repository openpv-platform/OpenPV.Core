using Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;
using Ahsoka.Services.Bluez.Gatt.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt;

internal class GattApplicationManager
{
    private readonly Connection connection;
    private readonly string adapterPath;
    private readonly Dictionary<string, GattCharacteristic> activeCharacteristicDict = new();

    public GattApplicationManager(Connection serverConnection, string bluezAdapterPath)
    {
        connection = serverConnection;
        adapterPath = bluezAdapterPath;
    }

    public async Task<IEnumerable<GattService>> RegisterGattApplication(string appName,
        IEnumerable<BLEServiceProperties> gattServiceDescriptions,
        LinuxImplementation serviceImplementation)
    {
        var applicationObjectPath = GenerateApplicationObjectPath(appName, gattServiceDescriptions.First().LocalName);
        var gattServices = await BuildApplicationTree(applicationObjectPath, gattServiceDescriptions, serviceImplementation);
        await RegisterApplicationInBluez(applicationObjectPath);

        return gattServices;
    }

    public Dictionary<string, GattCharacteristic> GetCharacteristicDictionary()
    {
        return activeCharacteristicDict;
    }

    private string GenerateApplicationObjectPath(string appName, string serviceName)
    {
        var appId = Guid.NewGuid().ToString().Substring(0, 8);
        var applicationObjectPath = $"{adapterPath}/{appName}/{serviceName}_{appId}";
        return applicationObjectPath;
    }

    private async Task<IEnumerable<GattService>> BuildApplicationTree(string applicationObjectPath, IEnumerable<BLEServiceProperties> gattServiceDescriptions,
       LinuxImplementation linuxImplementation)
    {
        List<GattService> gattServices = new();

        var application = await BuildGattApplication(applicationObjectPath);

        foreach (var serviceDescription in gattServiceDescriptions)
        {
            var service = await AddNewService(application, serviceDescription);
            gattServices.Add(service);

            foreach (var characteristicDescription in serviceDescription.Characteristics)
            {
                var characteristic = activeCharacteristicDict[characteristicDescription.Uuid] = await AddNewCharacteristic(service, characteristicDescription, linuxImplementation);
                foreach (var descriptorDescription in characteristicDescription.Descriptors)
                {
                    await AddNewDescriptor(characteristic, descriptorDescription);
                }
            }
        }

        return gattServices;
    }

    private async Task RegisterApplicationInBluez(string applicationObjectPath)
    {
        var gattManager = connection.CreateProxy<IGattManager1>("org.bluez", adapterPath);
        await gattManager.RegisterApplicationAsync(new ObjectPath(applicationObjectPath), new Dictionary<string, object>());
    }

    public async Task UnRegisterGattApplication(string applicationObjectPath)
    {
        var gattManager = connection.CreateProxy<IGattManager1>("org.bluez", adapterPath);
        await gattManager.UnregisterApplicationAsync(new ObjectPath(applicationObjectPath));
    }

    private async Task<GattApplication> BuildGattApplication(string applicationObjectPath)
    {
        var application = new GattApplication(applicationObjectPath);
        await connection.RegisterObjectAsync(application);
        return application;
    }

    private async Task<GattService> AddNewService(GattApplication application, BLEServiceProperties serviceDescription)
    {
        var gattService1Properties = GattPropertiesFactory.CreateGattService(serviceDescription);
        var gattService = application.AddService(gattService1Properties);
        await connection.RegisterObjectAsync(gattService);
        return gattService;
    }

    private async Task<GattCharacteristic> AddNewCharacteristic(GattService gattService, BLECharacteristicProperties characteristic, LinuxImplementation linuxImplementation)
    {
        var gattCharacteristic1Properties = GattPropertiesFactory.CreateGattCharacteristic(characteristic);
        var gattCharacteristic = gattService.AddCharacteristic(gattCharacteristic1Properties, characteristic, linuxImplementation);

        await connection.RegisterObjectAsync(gattCharacteristic);

        // If the characteristic property received has their name defined and there isn't already a descriptor defined
        // to set that value, then we will make the description property ourselves.
        if (characteristic.Name != ""
            && !characteristic.Descriptors.Exists(desc => desc.Uuid == ReadWriteUtility.CharacteristicUserDescriptionUUID))
        {
            var characteristicNameDescriptor = new BLECharacteristicDescriptorProperties()
            {
                Uuid = ReadWriteUtility.CharacteristicUserDescriptionUUID,
                Value = Encoding.UTF8.GetBytes(characteristic.Name),
            };
            characteristicNameDescriptor.Flags.Add(BLEDescriptorFlag.DescriptorRead);
            await AddNewDescriptor(gattCharacteristic, characteristicNameDescriptor);
        }

        return gattCharacteristic;
    }

    private async Task AddNewDescriptor(GattCharacteristic gattCharacteristic, BLECharacteristicDescriptorProperties descriptor)
    {
        var gattDescriptor1Properties = GattPropertiesFactory.CreateGattDescriptor(descriptor);
        var gattDescriptor = gattCharacteristic.AddDescriptor(gattDescriptor1Properties);
        await connection.RegisterObjectAsync(gattDescriptor);
    }
}