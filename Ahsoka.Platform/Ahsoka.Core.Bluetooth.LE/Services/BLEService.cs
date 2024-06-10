using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.Services.Bluetooth.LE;

[AhsokaService(Name)]
internal class BLEService : AhsokaServiceBase<BLEMessageTypes.Ids>
{
    public const string Name = "BLEService";
    readonly ImplementationBase bluetoothImplementation;

    public BLEService()
        : this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public BLEService(ServiceConfiguration config) :
        base(config, new BLEServiceMessages())
    {
        bluetoothImplementation = GetInterop();
        bluetoothImplementation.InitServiceImplementation(this);
    }

    internal static ImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.OpenViewLinux => new LinuxImplementation(),
            PlatformFamily.Windows64 => new WindowsImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<BLEMessageTypes.Ids>())
        {
            case BLEMessageTypes.Ids.EnableAdapter:
                if (message is BLEAdapterRequest enableAdapterTarget)
                    EnableAdapter(messageHeader, enableAdapterTarget);
                break;

            case BLEMessageTypes.Ids.DisableAdapter:
                if (message is BLEAdapterRequest disableAdapterTarget)
                    DisableAdapter(messageHeader, disableAdapterTarget);
                break;

            case BLEMessageTypes.Ids.IsAdapterEnabled:
                if (message is BLEAdapterRequest isAdapterEnabledTarget)
                    IsAdapterEnabled(messageHeader, isAdapterEnabledTarget);
                break;

            case BLEMessageTypes.Ids.CreateAdvertisement:
                if (message is BLEAdvertisementProperties info)
                    CreateAdvertisement(messageHeader, info);
                break;

            case BLEMessageTypes.Ids.RemoveAdvertisement:
                if (message is BLEAdvertisementInfo adInfo)
                    RemoveAdvertisement(messageHeader, adInfo);
                break;

            case BLEMessageTypes.Ids.CreateService:
                if (message is BLEServiceProperties createBLEServiceProperties)
                    CreateService(messageHeader, createBLEServiceProperties);
                break;

            case BLEMessageTypes.Ids.RemoveService:
                if (message is BLEServiceInfo deleteBLEServiceProperties)
                    RemoveService(messageHeader, deleteBLEServiceProperties);
                break;

            case BLEMessageTypes.Ids.SetCharacteristicValue:
                if (message is BLECharacteristicValue newValueProperty)
                    SetCharacteristicValue(messageHeader, newValueProperty);
                break;


            case BLEMessageTypes.Ids.CharacteristicValueReadResult:
                if (message is BLECharacteristicValue newValueResult)
                    QueueCharacteristicValue(messageHeader, newValueResult);
                break;

            case BLEMessageTypes.Ids.SendCharacteristicNotification:
                if (message is BLECharacteristicInfo characteristicIdentifier)
                    SendCharacteristicNotification(messageHeader, characteristicIdentifier);
                break;

            case BLEMessageTypes.Ids.ConfirmPairingRequest:
                if (message is BLEPairingConfirmation confirmation)
                    ConfirmPairing(messageHeader, confirmation);
                break;
        }
    }

    private void EnableAdapter(AhsokaMessageHeader messageHeader, BLEAdapterRequest adapterInfo)
    {
        // Send nothing as an ACK that we're enabling the adapter.
        bluetoothImplementation.Enable(adapterInfo);
        SendMessageInternal(messageHeader);
    }

    private void DisableAdapter(AhsokaMessageHeader messageHeader, BLEAdapterRequest adapterInfo)
    {
        bluetoothImplementation.Disable(adapterInfo);
        SendMessageInternal(messageHeader);
    }
    private void IsAdapterEnabled(AhsokaMessageHeader messageHeader, BLEAdapterRequest adapterInfo)
    {
        bool isEnabled = bluetoothImplementation.IsEnabled(adapterInfo);

        BLEIsAdapterEnabledResponse response = new()
        {
            LocalName = adapterInfo.LocalName,
            IsEnabled = isEnabled,
        };
        SendMessageInternal(messageHeader, response);
    }

    private void CreateAdvertisement(AhsokaMessageHeader messageHeader, BLEAdvertisementProperties advertisementProperties)
    {
        bluetoothImplementation.CreateAdvertisement(advertisementProperties);
        SendMessageInternal(messageHeader);
    }

    private void RemoveAdvertisement(AhsokaMessageHeader messageHeader, BLEAdvertisementInfo advertisementInfo)
    {
        bluetoothImplementation.RemoveAdvertisement(advertisementInfo);
        SendMessageInternal(messageHeader);
    }

    private void CreateService(AhsokaMessageHeader messageHeader, BLEServiceProperties serviceProperties)
    {
        bluetoothImplementation.CreateService(serviceProperties);
        SendMessageInternal(messageHeader);
    }

    private void RemoveService(AhsokaMessageHeader messageHeader, BLEServiceInfo serviceProperties)
    {
        bluetoothImplementation.RemoveService(serviceProperties);
        SendMessageInternal(messageHeader);
    }

    private void SetCharacteristicValue(AhsokaMessageHeader messageHeader, BLECharacteristicValue newValueProperty)
    {
        bluetoothImplementation.SetCharacteristicValue(newValueProperty);
        SendMessageInternal(messageHeader);
    }

    private void QueueCharacteristicValue(AhsokaMessageHeader messageHeader, BLECharacteristicValue newValueResult)
    {
        bluetoothImplementation.QueueCharacteristicValue(newValueResult);
        SendMessageInternal(messageHeader);
    }

    private void SendCharacteristicNotification(AhsokaMessageHeader messageHeader, BLECharacteristicInfo characteristicIdentifier)
    {
        bluetoothImplementation.SendCharacteristicNotification(characteristicIdentifier);
        SendMessageInternal(messageHeader);
    }


    private void ConfirmPairing(AhsokaMessageHeader messageHeader, BLEPairingConfirmation pairingConfirmation)
    {
        bluetoothImplementation.ConfirmPairing(pairingConfirmation);
        SendMessageInternal(messageHeader);
    }

    internal void SendNotification(BLEMessageTypes.Ids transportID, object notification)
    {
        AhsokaMessageHeader header = new()
        {
            TransportId = transportID.EnumToInt(),
        };
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"BLE Service: SendNotification {Enum.GetName(typeof(BLEMessageTypes.Ids), header.TransportId)}");
        SendMessageInternal(header, notification, true);
    }

}
