using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ahsoka.Services.Bluetooth.LE;

/// <summary>
/// Base View Model for building Bluetooth LE Services
/// </summary>
public abstract class BLEServiceViewModelBase : ViewModelBase
{
    #region Fields
    private bool serviceEnabled = false;
    private BLEServiceClient client;
    readonly List<BLECharacteristicValue> queuedEvents = new();
    readonly Dictionary<string, CharacteristicInfo> propertyHandlers = new();
    #endregion

    #region Methods
    /// <summary>
    /// Registered Service
    /// </summary>
    public void EnableService(BLEServiceClient client, BLEAdvertisementProperties advertInfo)
    {
        this.client = client;

        // Get Service Info and Register with Service Service
        GetServiceInfo(out BLEServiceProperties props, advertInfo);
        this.client.CreateService(props);
        serviceEnabled = true;

        // Queue Updates
        foreach (var item in queuedEvents)
            this.client.SetCharacteristicValue(item);

        queuedEvents.Clear();

        // Listen to Model Updates
        this.client.NotificationReceived += NotificationReceived;
    }

    private void NotificationReceived(object sender, AhsokaClientBase<BLEMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == BLEMessageTypes.Ids.CharacteristicValueWrite
            && e.NotificationObject is BLECharacteristicValue newValue)
        {
            HandleWriteRequest(newValue);
        }
        else if (e.TransportId == BLEMessageTypes.Ids.CharacteristicValueRead
                && e.NotificationObject is BLECharacteristicValue requestValue)
        {
            HandleReadRequest(requestValue);
        }
    }

    /// <summary>
    /// De-Register Service with System
    /// </summary>
    public void DisableService()
    {
        this.client.NotificationReceived -= NotificationReceived;

        var attr = GetServiceAttribute();
        this.client.RemoveService(new BLEServiceInfo() { Uuid = attr.UUID });
        this.client = null;
        serviceEnabled = false;
    }

    /// <summary>
    /// Default Constructor for registering a servie with a 
    /// </summary>
    public BLEServiceViewModelBase()
    {

    }

    /// <summary>
    /// Raised after a Write is Handled by the Model.
    /// </summary>
    /// <param name="updateNotification"></param>
    protected virtual void OnHandleWriteRequest(BLECharacteristicValue updateNotification) { }
    #endregion

    #region Protected Getter / Setters
    /// <inheritdoc />
    protected override void OnSetObject<T>(ref T baseValue, T newValue, [CallerMemberName] string memberName = "")
    {
        if (baseValue == null || !baseValue.Equals(newValue))
        {
            base.OnSetObject(ref baseValue, newValue, memberName);
            HandleSetValue(newValue, memberName);
        }
    }

    /// <inheritdoc />
    protected override void OnSetValue<T>(ref T baseValue, T newValue, [CallerMemberName] string memberName = "")
    {
        if (!baseValue.Equals(newValue))
        {
            base.OnSetValue(ref baseValue, newValue, memberName);
            HandleSetValue(newValue, memberName);
        }
    }

    private void HandleSetValue(object newValue, string memberName)
    {
        var charAttr = this.GetType().GetProperty(memberName).GetCustomAttribute<BLECharacteristicAttribute>();
        if (charAttr != null)
        {
            if (serviceEnabled)
            {
                // Update Characteristic
                client.SetCharacteristicValue(new BLECharacteristicValue() { Uuid = charAttr.UUID, Value = GetData(newValue) });

                // Send a Notification if this Characteristic has Notification Enabled
                if (charAttr.Flags.Contains(BLECharacteristicFlag.Notify))
                    client.SendCharacteristicNotification(new BLECharacteristicInfo() { Uuid = charAttr.UUID });
            }
            else
                queuedEvents.Add(new BLECharacteristicValue() { Uuid = charAttr.UUID, Value = GetData(newValue) }); // Will be run after Startup.
        }
    }
    #endregion

    #region Service Listeners (Read / Write) 
    void HandleReadRequest(BLECharacteristicValue requestValue)
    {
        if (propertyHandlers.TryGetValue(requestValue.Uuid, out var handler))
        {
            if (handler.ReadMethod != null)
            {
                handler.ReadMethod?.Invoke(this, new object[1] { requestValue });
                client.QueueCharacteristicValue(requestValue);
            }
        }
    }

    void HandleWriteRequest(BLECharacteristicValue updateNotification)
    {
        if (propertyHandlers.TryGetValue(updateNotification.Uuid, out var handler))
        {
            if (handler.PropertyInfo != null)
            {
                handler.PropertyInfo.SetValue(this, GetValue(handler.PropertyInfo.PropertyType, updateNotification.Value));
                OnHandleWriteRequest(updateNotification);
            }
            else if (handler.WriteMethod != null)
            {
                handler.WriteMethod.Invoke(this, new object[1] { updateNotification });
                OnHandleWriteRequest(updateNotification);
            }
        }
    }
    #endregion


    #region Conversion Methods
    private static byte[] GetData(object newValue)
    {
        if (newValue == null)
            return Array.Empty<byte>();

        byte[] data;
        if (newValue is string myString)
            data = Encoding.UTF8.GetBytes(myString);
        else if (newValue is byte[] bytes)
            data = bytes;
        else if (newValue is int numVal32)
            data = BitConverter.GetBytes(numVal32);
        else if (newValue is short numVal16)
            data = BitConverter.GetBytes(numVal16);
        else if (newValue is byte numVal8)
            data = new byte[1] { numVal8 };
        else if (newValue is float floatVal)
            data = BitConverter.GetBytes(floatVal);
        else if (newValue is double doubleVal)
            data = BitConverter.GetBytes(doubleVal);
        else
            throw new ApplicationException($"Data type of {newValue.GetType()} is not supported by BluetoothViewModel");

        return data;
    }

    private static object GetValue(Type type, byte[] data)
    {
        if (type == typeof(string))
            return Encoding.UTF8.GetString(data);
        else if (type == typeof(byte[]))
            return data;
        else if (type == typeof(int))
            return BitConverter.ToInt32(data);
        else if (type == typeof(short))
            return BitConverter.ToInt16(data);
        else if (type == typeof(byte))
            return data[0];
        else if (type == typeof(double))
            return BitConverter.ToDouble(data);
        else if (type == typeof(float))
            return BitConverter.ToSingle(data);
        else
            throw new ApplicationException($"Data type of {type} is not supported by BluetoothViewModel");
    }
    #endregion

    #region Private Lookup Methods
    private void GetServiceInfo(out BLEServiceProperties serviceProps, BLEAdvertisementProperties advertInfo)
    {
        var attr = GetServiceAttribute();

        serviceProps = new BLEServiceProperties()
        {
            Uuid = attr.UUID,
            Primary = attr.IsPrimary,
            LocalName = attr.Name
        };

        // Add to Advert List
        if (attr.Advertise)
        {
            advertInfo.ServiceUuids.Add(attr.UUID);

            var mfg = GetManufacturingDataAttribute();
            if (mfg != null)
                advertInfo.ManufactureDatas.Add(mfg.Key, mfg.Data);
        }

        // Add all Characteristics.
        LoadCharacteristiceAttributes();
        foreach (var item in propertyHandlers)
        {
            // Create Characteristic
            var characteristic = new BLECharacteristicProperties()
            {
                Uuid = item.Key,
                Name = item.Value.Attribute.Name, // Note that App Manager will add a Name Descriptor for us.
                UseNotifyForRead = item.Value.ReadMethod != null,
                UseNotifyForWrite = item.Value.WriteMethod != null
            };

            characteristic.Flags.AddRange(item.Value.Attribute.Flags);
            serviceProps.Characteristics.Add(characteristic);
        }
    }
    internal BLEServiceAttribute GetServiceAttribute()
    {
        return this.GetType().GetCustomAttribute<BLEServiceAttribute>();
    }

    internal BLEManufactureDataAttribute GetManufacturingDataAttribute()
    {
        return this.GetType().GetCustomAttribute<BLEManufactureDataAttribute>();
    }

    private void LoadCharacteristiceAttributes()
    {
        propertyHandlers.Clear();

        foreach (var propInfo in this.GetType().GetProperties())
        {
            var charAttr = propInfo.GetCustomAttribute<BLECharacteristicAttribute>();
            if (charAttr != null)
                propertyHandlers.Add(charAttr.UUID, new() { Attribute = charAttr, PropertyInfo = propInfo });
        }

        foreach (var methodInfo in this.GetType().GetMethods())
        {
            var charAttr = methodInfo.GetCustomAttribute<BLECharacteristicAttribute>();
            if (charAttr != null)
            {
                if (methodInfo.ReturnType != typeof(void) || (methodInfo.GetParameters().First().ParameterType != typeof(BLECharacteristicValue)))
                    throw new MissingMethodException("Methods for BluetoothCharacteristic Attributes must return void and take a single BLECharacteristicValue Arguement");

                if (!propertyHandlers.TryGetValue(charAttr.UUID, out var characteristic))
                {
                    propertyHandlers[charAttr.UUID] = characteristic = new CharacteristicInfo() { Attribute = charAttr };
                }
                else
                {
                    if (characteristic.PropertyInfo != null)
                        throw new ApplicationException($"Property Info Found for Characteristic {charAttr.UUID} which is also has a Write or Read Attribute. Either Properties or Methods for a Characteristic but not both for a single UUID");

                    // Validate Attributes Match
                    if (charAttr.Name != characteristic.Attribute.Name)
                        throw new ApplicationException($"Read and Write Attributes for Characteristic {charAttr.UUID} not match");

                    // Combine Flags
                    foreach (var item in charAttr.Flags)
                        if (!characteristic.Attribute.Flags.Contains(item))
                            characteristic.Attribute.Flags.Add(item);
                }

                if (charAttr is BLECharacteristicWriteMethodAttribute)
                    characteristic.WriteMethod = methodInfo;
                else if (charAttr is BLECharacteristicReadMethodAttribute)
                    characteristic.ReadMethod = methodInfo;
            }
        }
    }
    #endregion

    class CharacteristicInfo
    {
        public BLECharacteristicAttribute Attribute;
        public PropertyInfo PropertyInfo;
        public MethodInfo ReadMethod;
        public MethodInfo WriteMethod;
    }
}
