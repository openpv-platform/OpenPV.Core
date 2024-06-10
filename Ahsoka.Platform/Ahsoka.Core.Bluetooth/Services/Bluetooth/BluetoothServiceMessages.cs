using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Bluetooth;

internal class BluetoothServiceMessages : AhsokaMessagesBase
{
    public BluetoothServiceMessages()
    {
        // Configure Adapters
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.GetBluetoothConfiguration, typeof(EmptyNotification), typeof(BluetoothConfiguration));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SetBluetoothConfiguration, typeof(BluetoothConfiguration), typeof(EmptyNotification));

        // Enable Adapters
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.EnableAdapter, typeof(BluetoothAdapterRequest), typeof(EmptyNotification));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.DisableAdapter, typeof(BluetoothAdapterRequest), typeof(EmptyNotification));

        // Set State
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.RequestAdapterState, typeof(BluetoothAdapterRequest), typeof(BluetoothAdapterState));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.UpdateAdapterState, typeof(BluetoothAdapterState), typeof(EmptyNotification));

        // Discover Devices
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.StartDiscovery, typeof(BluetoothAdapterRequest), typeof(EmptyNotification));
        this.RegisterServiceNotification(BluetoothMessageTypes.Ids.DiscoveryDeviceNotification, typeof(BluetoothDeviceDiscoveryInfo));

        // Pairing
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.RequestToPair, typeof(BluetoothDeviceDiscoveryInfo), typeof(BluetoothDevicePairedInfo));
        //       this.RegisterServiceRequest(BluetoothMessageTypes.Ids.CompletePairRequest, typeof(BluetoothDevicePairedInfo), typeof(BluetoothDevicePairedInfo));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.RequestToUnpair, typeof(BluetoothDevicePairedInfo), typeof(EmptyNotification));
        this.RegisterServiceNotification(BluetoothMessageTypes.Ids.PairedDeviceNotification, typeof(BluetoothDevicePairedInfo));

        // Manage Paired Devices
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.GetPairedDevices, typeof(BluetoothAdapterRequest), typeof(BluetoothPairedDevices));
        //        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.GetPairedDevicesResponse, typeof(BluetoothPairedDevices));
        //        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SetPairedDevices, typeof(BluetoothPairedDevices));

        // Connect Devices
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.RequestConnect, typeof(BluetoothConnectionRequest), typeof(ProfileConnectionState));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.RequestDisconnect, typeof(BluetoothConnectionRequest), typeof(EmptyNotification));
        this.RegisterServiceNotification(BluetoothMessageTypes.Ids.ProfileConnectionStateChanged, typeof(ProfileConnectionState));

        /*
        // Test Modes
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.StartTestMode, typeof(BluetoothTestConfiguration));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.StopTestMode, typeof(BluetoothAdapterRequest));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.ConfigureLogging, typeof(BluetoothLoggingConfiguration));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.DisableLogging, typeof(BluetoothAdapterRequest));

        // HFP Call Management
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.GetPhoneStatus, typeof(BluetoothAdapterRequest));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.GetPhoneStatusResponse, typeof(PhoneStatus));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.PhoneCallNotification, typeof(Call));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.PhoneStatusChangedNotification, typeof(PhoneStatus));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SendPhoneCommand, typeof(CallCommand));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SendPhoneCommandResponse, typeof(CallCommandResponse));

        // Audio Management
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.StartListenForSongInfo, typeof(BluetoothAdapterRequest));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.StopListenForSongInfo, typeof(BluetoothAdapterRequest));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SongInfoChanged, typeof(CurrentSongInfo));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.ConnectAudio, typeof(BluetoothAdapterRequest));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.DisconnectAudio, typeof(BluetoothAdapterRequest));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SendMusicCommand, typeof(MusicCommand));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SendMusicCommandResponse, typeof(MusicCommandResponse));

        // Basic Message Access Profile.
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SendTextMessage, typeof(TextMessage));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SendTextMessageResponse, typeof(TextMessageReponse));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.TextMessageReceived, typeof(TextMessage));


        // iAP
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.iAPSessionBegin, typeof(iAPNotification));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.iAPSessionEnded, typeof(iAPNotification));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.iAPLaunchPhoneApp, typeof(iAPLaunchApp));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.iAPDataRecieved, typeof(iAPData));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.iAPSendData, typeof(iAPData));

        // SPP
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SPPDataRecieved, typeof(SPPData));
        this.RegisterServiceRequest(BluetoothMessageTypes.Ids.SPPSendData, typeof(SPPData));*/
    }

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\BluetoothServiceAudio.proto", Properties.BluetoothResources.BluetoothServiceAudio);
        result.Add("Ahsoka.Proto\\BluetoothServiceData.proto", Properties.BluetoothResources.BluetoothServiceData);
        result.Add("Ahsoka.Proto\\BluetoothServiceMessageAccess.proto", Properties.BluetoothResources.BluetoothServiceMessageAccess);
        result.Add("Ahsoka.Proto\\BluetoothServicePhonebook.proto", Properties.BluetoothResources.BluetoothServicePhonebook);
        result.Add("Ahsoka.Proto\\BluetoothServicePhoneCall.proto", Properties.BluetoothResources.BluetoothServicePhoneCall);
        result.Add("Ahsoka.Proto\\BluetoothServiceService.proto", Properties.BluetoothResources.BluetoothServiceService);
        return result;
    }
}
