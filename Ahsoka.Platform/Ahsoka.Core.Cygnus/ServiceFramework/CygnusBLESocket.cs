using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using Ahsoka.Services.Bluetooth.LE;
using Microsoft.Extensions.Azure;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// 
/// </summary>
public sealed class CygnusBLEServiceSocket : CygnusSerialSocket
{
    // We only want timeout to be a maximum of 1 second because this socket is always going to be running on the same
    // system that is running the BLE.
    private int bleTimeout = 1000;
    private BLEServiceClient bleClient = new BLEServiceClient();
    byte[] lastReceiveValue;

    bool BLEServiceReady = false;

    BLECharacteristicProperties cyngusMainCommsCharacteristic = new();
    BLECharacteristicValue cyngusMainCommsCharacteristicValue = new();
    BLECharacteristicInfo cyngusMainCommsCharacteristicIdentifier = new();
    BLEServiceProperties cyngusServiceDefinition = new();

    BLEAdvertisementProperties cygnusAdvertisementProperties = new();

    /// <summary>
    /// 
    /// </summary>
    public CygnusBLEServiceSocket()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    ~CygnusBLEServiceSocket()
    {
        bleClient.CurrentRuntime.RequestShutdown();
        bleClient.CurrentRuntime.WaitForShutdown();
    }

    private void SetupBLEService()
    {
        cyngusMainCommsCharacteristic.Name = "ReachAPI";
        cyngusMainCommsCharacteristic.Uuid = "d42d1039-1d11-4f10-bae6-5f3b44cf6439";
        cyngusMainCommsCharacteristic.Flags.Add(BLECharacteristicFlag.Read);
        cyngusMainCommsCharacteristic.Flags.Add(BLECharacteristicFlag.WriteWithoutResponse);
        cyngusMainCommsCharacteristic.Flags.Add(BLECharacteristicFlag.Notify);

        cyngusMainCommsCharacteristicValue.Uuid = cyngusMainCommsCharacteristic.Uuid;
        cyngusMainCommsCharacteristicValue.Offset = 0;
        cyngusMainCommsCharacteristicIdentifier.Uuid = cyngusMainCommsCharacteristic.Uuid;

        cyngusServiceDefinition.LocalName = "ReachPV";
        cyngusServiceDefinition.Uuid = "edd59269-79b3-4ec2-a6a2-89bfb640f930";
        cyngusServiceDefinition.Primary = true;
        cyngusServiceDefinition.Characteristics.Add(cyngusMainCommsCharacteristic);

        cygnusAdvertisementProperties.Type = BLEAdvertisementType.Peripheral;
        cygnusAdvertisementProperties.ServiceUuids.Add(cyngusServiceDefinition.Uuid);

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
        bleClient.CreateService(cyngusServiceDefinition);
        // TODO: BMG (Nov. 22, 2023) I don't know if we can make changes whenever there is already an advertisement
        // present...
        bleClient.CreateAdvertisement(cygnusAdvertisementProperties);

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
        bleServiceInfo.Uuid = cyngusServiceDefinition.Uuid;
        bleClient.RemoveService(bleServiceInfo);

        IsConnected = false;
    }

    private void NotificationCallback(object sender, NotificationEventArgs<BLEMessageTypes.Ids> e)
    {
        if (e.TransportId == BLEMessageTypes.Ids.CharacteristicValueWrite)
        {
            BLECharacteristicValue writeValue = e.NotificationObject as BLECharacteristicValue;

            if (writeValue.Uuid == cyngusMainCommsCharacteristic.Uuid)
            {
                lastReceiveValue = writeValue.Value;
                ((IAhsokaServiceEndpoint)EndPoint).ReceiveMessage(true);
            }
        }
    }

    /// <summary>
    /// Recieve Data from Raw Transport
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
    /// Transmit Data over Raw Transport
    /// </summary>
    /// <param name="packetData"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    protected override bool TransmitOverInterface(byte[] packetData, byte[] clientId)
    {
        bool returnSuccess = true;

        try
        {
            cyngusMainCommsCharacteristicValue.Value = packetData;
            bleClient.SetCharacteristicValue(cyngusMainCommsCharacteristicValue, bleTimeout);
            bleClient.SendCharacteristicNotification(cyngusMainCommsCharacteristicIdentifier, bleTimeout);
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
