using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Bluetooth.LE;

internal class BLEServiceMessages : AhsokaMessagesBase
{
    public BLEServiceMessages()
    {
        // Enable Setup
        RegisterServiceRequest(BLEMessageTypes.Ids.EnableAdapter, typeof(BLEAdapterRequest), typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.DisableAdapter, typeof(BLEAdapterRequest), typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.IsAdapterEnabled, typeof(BLEAdapterRequest), typeof(BLEIsAdapterEnabledResponse));
        RegisterServiceNotification(BLEMessageTypes.Ids.AdapterEnabled, typeof(BLEConnectionNotification));
        RegisterServiceNotification(BLEMessageTypes.Ids.AdapterDisabled, typeof(BLEConnectionNotification));

        // Runtime Handling (Read, Notify, Write)
        RegisterServiceNotification(BLEMessageTypes.Ids.PairingRequested, typeof(BLEPairingRequest));
        RegisterServiceNotification(BLEMessageTypes.Ids.PairingCancelled, typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.ConfirmPairingRequest, typeof(BLEPairingConfirmation), typeof(EmptyNotification));

        // Service Setup
        RegisterServiceRequest(BLEMessageTypes.Ids.CreateAdvertisement, typeof(BLEAdvertisementProperties), typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.CreateService, typeof(BLEServiceProperties), typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.RemoveService, typeof(BLEServiceInfo), typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.RemoveAdvertisement, typeof(BLEAdvertisementInfo), typeof(EmptyNotification));

        // Manage Descriptor Values (Read, Notify, Write)
        RegisterServiceRequest(BLEMessageTypes.Ids.SetCharacteristicValue, typeof(BLECharacteristicValue), typeof(EmptyNotification));
        RegisterServiceRequest(BLEMessageTypes.Ids.SendCharacteristicNotification, typeof(BLECharacteristicInfo), typeof(EmptyNotification));

        // Characteristic Callbacks
        RegisterServiceNotification(BLEMessageTypes.Ids.CharacteristicValueWrite, typeof(BLECharacteristicValue));
        RegisterServiceNotification(BLEMessageTypes.Ids.CharacteristicValueRead, typeof(BLECharacteristicValue));
        RegisterServiceRequest(BLEMessageTypes.Ids.CharacteristicValueReadResult, typeof(BLECharacteristicValue), typeof(EmptyNotification));
    }
}
