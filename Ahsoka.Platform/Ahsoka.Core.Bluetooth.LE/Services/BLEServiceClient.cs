using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Services.Bluetooth.LE;

/// <summary>
/// Client for accessing Ahsoka Bluetooth LE Service
/// </summary>
public class BLEServiceClient : AhsokaClientBase<BLEMessageTypes.Ids>
{
    internal const string Name = "BLEService";

    /// <summary>
    /// Default Constructor
    /// </summary>
    public BLEServiceClient()
        : this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    /// <summary>
    /// Constructor for accepting customer configurations
    /// </summary>
    /// <param name="config"></param>
    public BLEServiceClient(ServiceConfiguration config) :
        base(config, new BLEServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Default Service Instance for In Proc and
    /// automatic service construction.
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new BLEService(this.ServiceConfig);
    }

    /// <summary>
    /// Model based method for creating and Registering a BLE Service 
    /// from a Model Object
    /// </summary>
    /// <param name="viewModels"></param>
    public void StartServiceModels(params BLEServiceViewModelBase[] viewModels)
    {
        BLEAdvertisementProperties advertInfo = new() { Type = BLEAdvertisementType.Peripheral };

        foreach (BLEServiceViewModelBase service in viewModels)
            service.EnableService(this, advertInfo);

        if (advertInfo.ServiceUuids.Count > 0)
            CreateAdvertisement(advertInfo);
    }

    /// <summary>
    /// UnRegisters a service for the passed in Model Objects
    /// </summary>
    /// <param name="viewModels"></param>
    public void StopServiceModels(params BLEServiceViewModelBase[] viewModels)
    {
        RemoveAdvertisement(new BLEAdvertisementInfo());

        foreach (BLEServiceViewModelBase service in viewModels)
            service.DisableService();
    }


    /// <summary>
    ///  Turns on and Enables the Adapter Specified
    /// </summary>
    public void EnableAdapter(string localName)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.EnableAdapter, new BLEAdapterRequest() { LocalName = localName });
    }

    /// <summary>
    /// Turns off and Disables the Adapter Specfied
    /// </summary>
    public void DisableAdapter()
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.DisableAdapter, new BLEAdapterRequest());
    }

    /// <summary>
    /// Returns the current enabled state of the interface
    /// </summary>
    public bool IsAdapterEnabled()
    {
        BLEIsAdapterEnabledResponse response =
            SendNotificationWithResponse<BLEIsAdapterEnabledResponse>(BLEMessageTypes.Ids.IsAdapterEnabled, new BLEAdapterRequest());
        return response.IsEnabled;
    }

    /// <summary>
    /// Creates an Advertisement
    /// </summary>
    /// <param name="advertisementProperties"></param>
    /// <param name="timeoutInMs"></param>
    public void CreateAdvertisement(BLEAdvertisementProperties advertisementProperties, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.CreateAdvertisement, advertisementProperties, timeoutInMs);
    }

    /// <summary>
    /// Removes Advertisement
    /// </summary>
    /// <param name="advertisementInfo"></param>
    /// <param name="timeoutInMs"></param>
    public void RemoveAdvertisement(BLEAdvertisementInfo advertisementInfo, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.RemoveAdvertisement, advertisementInfo, timeoutInMs);
    }

    /// <summary>
    /// Creates a BLE Service in Ahsoka BLE Service Manager 
    /// </summary>
    /// <param name="serviceProperties"></param>
    /// <param name="timeoutInMs"></param>
    public void CreateService(BLEServiceProperties serviceProperties, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.CreateService, serviceProperties, timeoutInMs);
    }

    /// <summary>
    /// Removes Service from Ahsoka Service Manager 
    /// </summary>
    /// <param name="serviceProperties"></param>
    /// <param name="timeoutInMs"></param>
    public void RemoveService(BLEServiceInfo serviceProperties, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.RemoveService, serviceProperties, timeoutInMs);
    }

    /// <summary>
    /// Sets the value of a Characteristic Stored in the BLE Service Manager
    /// </summary>
    /// <param name="newValueProperty"></param>
    /// <param name="timeoutInMs"></param>
    public void SetCharacteristicValue(BLECharacteristicValue newValueProperty, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.SetCharacteristicValue, newValueProperty, timeoutInMs);
    }

    /// <summary>
    /// Queue the value of a Characteristic Stored in the BLE Service Manager
    /// Called when the client receives a request to Read the Characteristic.
    /// </summary>
    /// <param name="newValueProperty"></param>
    /// <param name="timeoutInMs"></param>
    public void QueueCharacteristicValue(BLECharacteristicValue newValueProperty, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.CharacteristicValueReadResult, newValueProperty, timeoutInMs);
    }

    /// <summary>
    /// Notify the Central that the Characteristic has been updated
    /// by the peripheral.
    /// </summary>
    /// <param name="characteristicIdentifier"></param>
    /// <param name="timeoutInMs"></param>
    public void SendCharacteristicNotification(BLECharacteristicInfo characteristicIdentifier, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.SendCharacteristicNotification, characteristicIdentifier, timeoutInMs);
    }

    /// <summary>
    /// Handle Confirmations for Pairing after PairingAgent has requested
    /// confirmation based on pin, passkey or simple authorization.
    /// </summary>
    /// <param name="accepted"></param>
    /// <param name="optionalMessage"></param>
    /// <param name="timeoutInMs"></param>
    public void ConfirmPairingRequests(bool accepted, string optionalMessage, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<EmptyNotification>(BLEMessageTypes.Ids.ConfirmPairingRequest, new BLEPairingConfirmation() { Confirmed = accepted, Message = optionalMessage }, timeoutInMs);
    }
}
