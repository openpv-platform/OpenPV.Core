using Ahsoka.Services.Bluetooth.LE;
using System;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// A BLE socket implementation for Ahsoka Services.
/// </summary>
public sealed class BLEServiceSocket : AhsokaSerialSocket
{
    // We only want timeout to be a maximum of 1 second because this socket is always going to be running on the same
    // system that is running the BLE.
    private int bleTimeout = 1000;
    private BLEServiceClient bleClient = new BLEServiceClient();
    byte[] lastReceiveValue;

    bool BLEServiceReady = false;

    BLECharacteristicProperties ahsokaDataCommsCharacteristic = new();
    BLECharacteristicValue ahsokaDataCommsCharacteristicValue = new();
    BLECharacteristicInfo ahsokaDataCommsCharacteristicIdentifier = new();
    BLEServiceProperties ahsokaServiceDefinition = new();

    BLEAdvertisementProperties ahsokaAdvertisementProperties = new();

    /// <summary>
    /// Constructor for Service Socket
    /// </summary>
    public BLEServiceSocket()
    {

    }

    /// <summary>
    /// Destruct
    /// </summary>
    ~BLEServiceSocket()
    {
        bleClient.CurrentRuntime.RequestShutdown();
        bleClient.CurrentRuntime.WaitForShutdown();
    }

    private void SetupBLEService()
    {
        if (String.IsNullOrEmpty(EndPoint.ServiceConfig.BLEServiceUUID))
            throw new Exception("The service configuration provided doesn't support a BLE endpoint.");

        ahsokaDataCommsCharacteristic.Name = EndPoint.ServiceConfig.ServiceName;
        ahsokaDataCommsCharacteristic.Uuid = EndPoint.ServiceConfig.BLECharacteristicUUID;
        ahsokaDataCommsCharacteristic.Flags.Add(BLECharacteristicFlag.Read);
        ahsokaDataCommsCharacteristic.Flags.Add(BLECharacteristicFlag.WriteWithoutResponse);
        ahsokaDataCommsCharacteristic.Flags.Add(BLECharacteristicFlag.Notify);

        ahsokaDataCommsCharacteristicValue.Uuid = ahsokaDataCommsCharacteristic.Uuid;
        ahsokaDataCommsCharacteristicValue.Offset = 0;
        ahsokaDataCommsCharacteristicIdentifier.Uuid = ahsokaDataCommsCharacteristic.Uuid;

        ahsokaServiceDefinition.LocalName = "Ahsoka";
        ahsokaServiceDefinition.Uuid = EndPoint.ServiceConfig.BLEServiceUUID;
        ahsokaServiceDefinition.Primary = true;
        ahsokaServiceDefinition.Characteristics.Add(ahsokaDataCommsCharacteristic);

        ahsokaAdvertisementProperties.Type = BLEAdvertisementType.Peripheral;
        ahsokaAdvertisementProperties.ServiceUuids.Add(ahsokaServiceDefinition.Uuid);

        AhsokaRuntime.CreateBuilder().AddClients(bleClient).StartLocalRuntime();

        BLEServiceReady = true;
    }

    /// <summary>
    /// Connect the BLE Socket
    /// </summary>
    protected override void OnConnect(IAhsokaEndPoint endPoint)
    {
        this.EndPoint = endPoint;
        //  TODO: BMG (Oct. 29, 2023) Ask Jake if this is how we should be handling this type of scenario.
        if (IsConnected)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "The BLEServiceSocket is already connected.");
            return;
        }

        if (!BLEServiceReady)
        {
            SetupBLEService();
        }

        if (!bleClient.IsAdapterEnabled())
        {
            // NOTE: BMG (Oct. 29, 2023) This is here primarily because the BLE service isn't going to work and allow for things to be done 
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "The BLE Adapter for this device has not been enabled yet.");
            return;
        }

        // TODO: BMG (Oct. 29, 2023) Need to test out that if we create the service after the advertisement has been
        // started for the BLE device. We are going to move forward for the time being with the assumption that doing
        // this is possible.
        bleClient.CreateService(ahsokaServiceDefinition);
        // TODO: BMG (Nov. 22, 2023) I don't know if we can make changes whenever there is already an advertisement
        // present...
        bleClient.CreateAdvertisement(ahsokaAdvertisementProperties);

        bleClient.NotificationReceived += NotificationCallback;

        IsConnected = true;
    }

    /// <summary>
    /// Disconnect the BLE Socket
    /// </summary>
    protected override void OnDisconnect()
    {
        if (!IsConnected)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "The BLEServiceSocket is not connected.");
            return;
        }

        if (!bleClient.IsAdapterEnabled())
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "The BLE Adapter for this device has not been enabled yet.");
            return;
        }

        BLEServiceInfo bleServiceInfo = new();
        bleServiceInfo.Uuid = ahsokaServiceDefinition.Uuid;
        bleClient.RemoveService(bleServiceInfo);

        IsConnected = false;
    }

    private void NotificationCallback(object sender, NotificationEventArgs<BLEMessageTypes.Ids> e)
    {
        if (e.TransportId == BLEMessageTypes.Ids.CharacteristicValueWrite)
        {
            BLECharacteristicValue writeValue = e.NotificationObject as BLECharacteristicValue;

            if (writeValue.Uuid == ahsokaDataCommsCharacteristic.Uuid)
            {
                lastReceiveValue = writeValue.Value;
                ((IAhsokaServiceEndpoint)EndPoint).ReceiveMessage();
            }
        }
    }

    /// <summary>
    /// Recieve Data over Raw Interface
    /// </summary>
    /// <param name="packetData"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    protected override bool ReceiveOverInterface(out byte[] packetData, out byte[] clientId)
    {
        bool receiveValueReady = false;
        if (lastReceiveValue != null)
        {
            packetData = lastReceiveValue;
            receiveValueReady = true;
        }
        else
        {
            packetData = null;
        }

        clientId = null;

        return receiveValueReady;
    }

    /// <summary>
    /// Transmit Data over Raw Interface
    /// </summary>
    /// <param name="packetData"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    protected override bool TransmitOverInterface(byte[] packetData, byte[] clientId)
    {
        bool returnSuccess = true;

        try
        {
            ahsokaDataCommsCharacteristicValue.Value = packetData;
            bleClient.SetCharacteristicValue(ahsokaDataCommsCharacteristicValue, bleTimeout);
            bleClient.SendCharacteristicNotification(ahsokaDataCommsCharacteristicIdentifier, bleTimeout);
        }
        catch (Exception)
        {
            // TODO: BMG (Oct. 29, 2023) We likely have to do something here because if there was an exception there is
            // definitely a problem with the BLE service at this point.
            returnSuccess = false;
        }

        return returnSuccess;
    }
}
