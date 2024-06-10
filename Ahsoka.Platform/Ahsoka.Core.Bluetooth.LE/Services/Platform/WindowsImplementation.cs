using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Bluetooth.LE;

[ExcludeFromCodeCoverage]
internal class WindowsImplementation : ImplementationBase
{
    BLEService service;
    public WindowsImplementation() { }

    protected override void OnInit(BLEService service)
    {
        this.service = service;
    }

    protected override void OnEnableAdapter(BLEAdapterRequest adapterInfo)
    {
        // Notify Client of Adapter Ready
        var response = new BLEConnectionNotification() { State = BLEConnectionState.ClientReady };
        service.SendNotification(BLEMessageTypes.Ids.AdapterEnabled, response);

    }

    protected override void OnDisableAdapter(BLEAdapterRequest adapterInfo)
    {
        // Notify Client of Adapter Ready
        var response = new BLEConnectionNotification() { State = BLEConnectionState.ClientNotReady };
        service.SendNotification(BLEMessageTypes.Ids.AdapterEnabled, response);

    }

    protected override void OnCreateAdvertisement(BLEAdvertisementProperties BLEAdvertisementProperties)
    {
    }

    protected override void OnRemoveAdvertisement(BLEAdvertisementInfo advertisementInfo)
    {
    }

    protected override void OnCreateService(BLEServiceProperties BLEServiceProperties)
    {
    }

    protected override void OnRemoveService(BLEServiceInfo info)
    {
    }

    protected override void OnSetCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
    }

    protected override void OnSendCharacteristicNotification(BLECharacteristicInfo characteristicIdentifier)
    {
    }

    protected override void OnConfirmPairing(BLEPairingConfirmation pairingConfirmation)
    {
    }

    protected override void OnQueueCharacteristicValue(BLECharacteristicValue newValueProperty)
    {

    }
}
