using System;

namespace Ahsoka.Services.Bluetooth.LE;

internal abstract class ImplementationBase
{
    public BLEService Service { get; private set; }
    public bool isEnabled = false;

    public void InitServiceImplementation(BLEService service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
        OnInit(service);
    }

    public void Disable(BLEAdapterRequest adapterInfo)
    {
        isEnabled = false;
        OnDisableAdapter(adapterInfo);
    }

    public void Enable(BLEAdapterRequest adapterInfo)
    {
        isEnabled = true;
        OnEnableAdapter(adapterInfo);
    }

    public bool IsEnabled(BLEAdapterRequest adapterInfo)
    {
        return OnIsEnabledAdapter(adapterInfo);
    }

    public void CreateAdvertisement(BLEAdvertisementProperties BLEAdvertisementProperties)
    {
        OnCreateAdvertisement(BLEAdvertisementProperties);
    }

    public void RemoveAdvertisement(BLEAdvertisementInfo advertisementInfo)
    {
        OnRemoveAdvertisement(advertisementInfo);
    }

    public void CreateService(BLEServiceProperties serviceProperties)
    {
        OnCreateService(serviceProperties);
    }

    public void RemoveService(BLEServiceInfo serviceInfo)
    {
        OnRemoveService(serviceInfo);
    }

    public void SetCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
        OnSetCharacteristicValue(newValueProperty);
    }

    public void QueueCharacteristicValue(BLECharacteristicValue newValueProperty)
    {
        OnQueueCharacteristicValue(newValueProperty);
    }

    public void SendCharacteristicNotification(BLECharacteristicInfo characteristicIdentifier)
    {
        OnSendCharacteristicNotification(characteristicIdentifier);
    }

    internal void ConfirmPairing(BLEPairingConfirmation pairingConfirmation)
    {
        OnConfirmPairing(pairingConfirmation);
    }

    protected virtual bool OnIsEnabledAdapter(BLEAdapterRequest adapterInfo)
    {
        return isEnabled;
    }
    protected abstract void OnInit(BLEService service);
    protected abstract void OnEnableAdapter(BLEAdapterRequest adapterInfo);
    protected abstract void OnDisableAdapter(BLEAdapterRequest adapterInfo);
    protected abstract void OnSetCharacteristicValue(BLECharacteristicValue newValueProperty);
    protected abstract void OnQueueCharacteristicValue(BLECharacteristicValue newValueProperty);
    protected abstract void OnCreateAdvertisement(BLEAdvertisementProperties advertisementProperties);
    protected abstract void OnRemoveAdvertisement(BLEAdvertisementInfo advertisementInfo);
    protected abstract void OnCreateService(BLEServiceProperties serviceProperties);
    protected abstract void OnRemoveService(BLEServiceInfo serviceInfo);
    protected abstract void OnSendCharacteristicNotification(BLECharacteristicInfo characteristicIdentifier);
    protected abstract void OnConfirmPairing(BLEPairingConfirmation pairingConfirmation);
}