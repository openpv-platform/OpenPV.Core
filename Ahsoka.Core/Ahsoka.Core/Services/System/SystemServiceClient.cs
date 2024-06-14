using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.System;

/// <summary>
/// Client for talking to System Service
/// </summary>
public class SystemServiceClient : AhsokaClientBase<SystemMessageTypes.Ids>
{

    FileTransferClientHelper client;

    /// <summary>
    /// Constructor using Default Service Configuration
    /// </summary>
    public SystemServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(SystemService.Name))
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new SystemService(this.ServiceConfig);
    }

    /// <summary>
    /// Constructor for passing a custom Service Configuration
    /// </summary>
    /// <param name="config">Service Configuration used for Connecting to the Service</param>
    public SystemServiceClient(ServiceConfiguration config) :
        base(config, new SystemServiceMessages())
    {

    }


    /// <summary>
    /// Test Method used to validate communication by echoing a messsage back from the service.
    /// </summary>
    /// <param name="dataValue"></param>
    /// <returns></returns>
    public string EchoValue(string dataValue)
    {
        EchoValue request = new() { DataValue = dataValue };
        var result = SendNotificationWithResponse<EchoValue>(SystemMessageTypes.Ids.Echo, request);
        return result.DataValue;
    }

    /// <summary>
    /// Gets a list of Services running in the Service Runtime as well as their Runtime Information / Metrics.
    /// </summary>
    /// <returns></returns>
    public ServiceInfo GetRunningServices()
    {
        return SendNotificationWithResponse<ServiceInfo>(SystemMessageTypes.Ids.GetServiceInfo);
    }

    /// <summary>
    /// Requests that the Service Runtime Shut Down and Disconnect.  Also used to shutdown the system by passing "Reboot"
    /// </summary>
    /// <param name="type">Type of Shutdown</param>
    /// <param name="exitCode">An Exit Code to return from the process shutdown if applicable.</param>
    /// <param name="timeoutInMs"></param>
    public void RequestServiceShutdown(ShutdownTypes type = ShutdownTypes.ShutdownServices, int exitCode = 0, int timeoutInMs = int.MaxValue)
    {
        SendNotificationWithResponse<GetHardwareInformationReply>(SystemMessageTypes.Ids.RequestShutdown,
             new ShutdownInformation() { ShutdownType = type, ShutdownCode = exitCode }, timeoutInMs);
    }

    /// <summary>
    /// Gets information about the Current Hardware, Platfrom and Version information.
    /// </summary>
    /// <param name="timeoutInMs">How long to wait before returning from a request for Hardware Information</param>
    /// <returns></returns>
    public GetHardwareInformationReply GetHardwareInformation(int timeoutInMs = int.MaxValue)
    {
        return SendNotificationWithResponse<GetHardwareInformationReply>(SystemMessageTypes.Ids.GetHardwareInformation, null, timeoutInMs);
    }

    /// <summary>
    /// Method used to load a new application.   Requires that the Target is running the Installer UI or another application that can handle the 
    /// autoload message if applicable
    /// </summary>
    /// <param name="stream">File or Stream containing a opv Package</param>
    /// <param name="autoLoad">Indicates that the target should begin the install when the opv file is completely uploaded.</param>
    /// <param name="progress">Progress Callback for tracking the status of the upload.</param>
    /// <returns></returns>
    public Task<MessageReplyStatus> LoadApplicationAsync(Stream stream, bool autoLoad, IProgress<FileTransmitProgressInfo> progress)
    {
        client = new FileTransferClientHelper(this);

        var task = Task.Run(() =>
        {
            return client.LoadFile(stream, autoLoad, progress);
        });

        return task;
    }

    /// <summary>
    /// Cancels an Upload in Progress
    /// </summary>
    public void CancelLoadApplication()
    {
        client?.CancelTransfer();
    }

    /// <summary>
    /// Ask the System to Load the specified Application File (.opv)
    /// This will shutdown the customer application and execute the install.
    /// </summary>
    /// <param name="pathToFile"></param>
    /// <exception cref="ApplicationException"></exception>
    public void LoadApplicationInternal(string pathToFile)
    {
        if (File.Exists(pathToFile))
        {
            using var stream = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);

            var progressString = new Progress<FileTransmitProgressInfo>(info =>
            {
                Console.WriteLine($"InternalLoad: {info.PercentComplete}% - {info.Message}");
            });

            var result = LoadApplicationAsync(stream, true, progressString);

            // Wait and Check Result
            if (result.GetAwaiter().GetResult() != MessageReplyStatus.Success)
            {
                throw new ApplicationException($"Loading {pathToFile} for automatic install failed.");
            }
        }

        // Stop application to automatically start the Installer
        RequestServiceShutdown(ShutdownTypes.ShutdownApplication);
    }

    /// <summary>
    /// Returns the Brightness of the Current Primary Display
    /// </summary>
    /// <returns></returns>
    public BrightnessInfo GetBrightness()
    {
        return SendNotificationWithResponse<BrightnessInfo>(SystemMessageTypes.Ids.GetBrightness);
    }

    /// <summary>
    /// Sets the brightness of the Primary Display.  Note that method returns a brightness value
    /// as the requested value may not be valid for some targets (for example if the display uses 
    /// a less than 1 byte Brightness Value) and the accepted value will be returned if the requested value is 
    /// not valid.
    /// </summary>
    /// <param name="brightnessInfo"></param>
    /// <returns></returns>
    public BrightnessInfo SetBrightness(BrightnessInfo brightnessInfo)
    {
        return SendNotificationWithResponse<BrightnessInfo>(SystemMessageTypes.Ids.SetBrightness, brightnessInfo);
    }

    /// <summary>
    /// Returns the state of the Default USB Controller 
    /// </summary>
    /// <returns>Information about the current Mode (Device / Host) and the Mount Point</returns>
    public UsbInfo GetUsbInfo()
    {
        return SendNotificationWithResponse<UsbInfo>(SystemMessageTypes.Ids.GetUsbInfo);
    }

    /// <summary>
    /// Sets the USB State to the Specified Info. Used for executing a Role swap or changing the default mount point for USB Mass Storage Devices.
    /// </summary>
    /// <param name="usbInfo">Information about the current Mode (Device / Host) and the Mount Point</param>
    public void SetUsbInfo(UsbInfo usbInfo)
    {
        SendNotificationWithResponse<EmptyNotification>(SystemMessageTypes.Ids.SetUsbInfo, usbInfo);
    }

    /// <summary>
    /// Sets the Extended Configuration for the Onboard RS485 Interface
    /// Standard Serial Configurations are completed using the Language Native Serial API's
    /// </summary>
    /// <param name="serialRS485Config">Extended Configration Values to set such as RTS Before / After and any Custom Flags for the Port</param>
    public void SetRS485Config(Platform.SerialRS485 serialRS485Config)
    {
        // Convert the base structure back into the protobuf structure to send it over the line.
        var send_config = new RS485ConfigNotification()
        {
            Flags = serialRS485Config.flags,
            DelayRtsAfterSend = serialRS485Config.delay_rts_after_send,
            DelayRtsBeforeSend = serialRS485Config.delay_rts_before_send
        };

        SendNotificationWithResponse<EmptyNotification>(SystemMessageTypes.Ids.SetSerialConfig, send_config);
    }


    /// <summary>
    /// Updates the touchscreen firmware and configuration.
    /// Modes of operation:
    ///     No Arguments: Return VendorId and ProductId of touchscreen.
    ///     Configuration Only: Update configuration and return status.
    ///     Configuration and Fimrware: Update firmware and configuration and return status.
    ///     Firmware Only: Invalid.
    /// </summary>
    /// <param name="configurationFilename">Filename of configuration update file for touchscreen.</param>
    /// <param name="firmwareFilename">Filename of firmware update file for touchscreen.</param>
    /// <returns>
    /// If configuration and firmware are omitted, returns VendorId and ProductId of touchscreen only.
    /// </returns>
    public TouchTuneInfoReply UpdateTouchTune(string configurationFilename = "", string firmwareFilename = "")
    {
        var request = new TouchTuneInfo()
        {
            ConfigurationFileName = configurationFilename,
            FirmwareFileName = firmwareFilename
        };

        return SendNotificationWithResponse<TouchTuneInfoReply>(SystemMessageTypes.Ids.UpdateTouchTune, request);
    }

    /// <summary>
    /// Sets the Boot Logo (Splash) to a given a .png file
    /// </summary>
    /// <param name="bootLogoInfo">Boot logo info such as the path to the .png image file.</param>
    public void SetBootLogo(BootLogoInfo bootLogoInfo)
    {
        SendNotificationWithResponse<EmptyNotification>(SystemMessageTypes.Ids.SetBootLogo, bootLogoInfo);
    }

   
}
