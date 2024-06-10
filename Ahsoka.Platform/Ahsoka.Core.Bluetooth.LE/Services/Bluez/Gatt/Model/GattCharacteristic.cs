using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.LE.Bluez.Core;
using Ahsoka.Services.Bluez.Core;
using Ahsoka.Services.Bluez.Gatt.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

internal class GattCharacteristic : PropertiesBase<GattCharacteristic1Properties>, IGattCharacteristic1,
    IObjectManagerProperties
{
    public IList<GattDescriptor> Descriptors { get; } = new List<GattDescriptor>();
    public string UUID => Properties.UUID;
    public byte[] Value => Properties.Value;

    readonly bool useNotifyForRead;
    readonly bool useNotifyForWrite;
    readonly BlockingCollection<byte[]> readResults;

    private readonly LinuxImplementation serviceImplementation;

    public GattCharacteristic(ObjectPath objectPath, BLECharacteristicProperties serviceProperties, GattCharacteristic1Properties properties, LinuxImplementation serviceImplementation) :
        base(objectPath, properties)
    {
        this.serviceImplementation = serviceImplementation;
        // Capture Read / Write Handling Behaviors.
        if (serviceProperties != null)
        {
            useNotifyForRead = serviceProperties.UseNotifyForRead;
            useNotifyForWrite = serviceProperties.UseNotifyForWrite;

            if (useNotifyForRead)
                readResults = new BlockingCollection<byte[]>();
        }
    }

    internal void SetCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
        byte[] newValueArray = ReadWriteUtility.SetValueWithOffsetOnArray(Value ?? Array.Empty<byte>(),
                newValueProperty.Value,
                (ushort)newValueProperty.Offset);

        SetAsync("Value", newValueArray).GetAwaiter();
    }


    internal void QueueCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
        // We handle these "raw" an dassume the offset 
        // was handled by the Characteristic Value Read handler
        // in the client application.
        readResults.Add(newValueProperty.Value);
    }

    public Task<byte[]> ReadValueAsync(IDictionary<string, object> Options)
    {
        ushort offsetValue = 0;
        if (Options.ContainsKey(ReadWriteUtility.ReadOffsetKeyString))
            offsetValue = (ushort)Options[ReadWriteUtility.ReadOffsetKeyString];

        if (useNotifyForRead)
        {
            // Send a Notification to Get Read value
            serviceImplementation.Service.SendNotification(BLEMessageTypes.Ids.CharacteristicValueRead, new BLECharacteristicValue()
            {
                Uuid = UUID,
                Offset = offsetValue,
                Handled = false
            });

            // Block until we recieve the "Value"
            // The client is responsible for Updating this value in response to the CharacteristicValueRead
            // event we sent.
            var result = readResults.Take();
            return Task.FromResult(result);
        }
        else
        {
            var value = ReadWriteUtility.GetValueAtOffset(Value, offsetValue);
            return Task.FromResult(value);
        }

    }

    public Task WriteValueAsync(byte[] value, IDictionary<string, object> Options)
    {
        ushort offsetValue = 0;
        if (Options.ContainsKey(ReadWriteUtility.ReadOffsetKeyString))
            offsetValue = (ushort)Options[ReadWriteUtility.ReadOffsetKeyString];

        // Execute the Write Function Directly
        if (!useNotifyForWrite)
            SetCharacteristicValue(new BLECharacteristicValue() { Uuid = UUID, Value = value, Offset = offsetValue });

        // We always Notify,
        // Handled indicates if the client needs to manage the set
        return Task.Run(() =>
        {
            serviceImplementation.Service.SendNotification(BLEMessageTypes.Ids.CharacteristicValueWrite, new BLECharacteristicValue()
            {
                Uuid = UUID,
                Value = value,
                Offset = offsetValue,
                Handled = !useNotifyForWrite
            });
        });
    }

    public Task TriggerNotification()
    {
        return TriggerPropertyChanged("Value");
    }

    public Task StartNotifyAsync()
    {
        return Task.Run(() =>
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service: Characteristic {UUID.Substring(0, 8)} Set to Notifying");
            Properties.Notifying = true;
        });
    }

    public Task StopNotifyAsync()
    {
        return Task.Run(() =>
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service: Characteristic {UUID.Substring(0, 8)} Set to Not Notifying");
            Properties.Notifying = false;
        });
    }

    public IDictionary<string, IDictionary<string, object>> GetProperties()
    {
        return new Dictionary<string, IDictionary<string, object>>
        {
            {
                "org.bluez.GattCharacteristic1", new Dictionary<string, object>
                {
                    {"Service", Properties.Service},
                    {"UUID", Properties.UUID},
                    {"Flags", Properties.Flags},
                    {"Descriptors", Descriptors.Select(d => d.ObjectPath).ToArray()}
                }
            }
        };
    }

    public GattDescriptor AddDescriptor(GattDescriptor1Properties gattDescriptorProperties)
    {
        gattDescriptorProperties.Characteristic = ObjectPath;
        var gattDescriptor = new GattDescriptor(NextDescriptorPath(), gattDescriptorProperties);
        Descriptors.Add(gattDescriptor);
        return gattDescriptor;
    }

    private ObjectPath NextDescriptorPath()
    {
        return ObjectPath + "/descriptor" + Descriptors.Count;
    }

}