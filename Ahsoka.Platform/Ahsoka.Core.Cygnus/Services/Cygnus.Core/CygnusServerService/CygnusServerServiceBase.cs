using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.Services.System;
using Ahsoka.Utility;
using Cygnus.Reach;

namespace Ahsoka.Services.Cygnus;

// TODO: BMG (Nov. 03, 2023) Create the underlying service base that is going to be used for this class.
/// <summary>
/// 
/// </summary>
public partial class CygnusServerService
{
    #region Fields

    /// <summary>
    /// Singleton
    /// </summary>
    public static CygnusServerService Default = new CygnusServerService();

    /// <summary>
    /// Configuration Name for the Can Service
    /// </summary>
    public const string Name = "CygnusServerService";
    /// <summary>
    /// The Data service base origin name for Cygnus server Configuration data
    /// </summary>
    public const string ReachConfigSeparator = "_";
    #endregion

    internal static BufferSizes CygnusBufferSizes = new() {
        MaxMessageSize              = 244,
        BigDataBufferSize           = 194,
        ParameterBufferCount        = 32,
        NumMediumStructsInMsg       = 4,
        DeviceInfoLen               = 48,
        LongStringLen               = 32,
        CountParamIds               = 32,
        MediumStringLen             = 24,
        ShortStringLen              = 16,
        ParamInfoEnumCount          = 12,
        ServicesCount               = 8,
        PiEnumCount                 = 8,
        NumCommandsInResponse       = 6,
        CountParamDescInResponse    = 2,
    };

    private bool DataValuesReady = false;
    private int DataServiceTimeout = 10000;
    private DataServiceClient DataClient = new();

    /// <summary>
    /// Add a file to the set of files that are going to be exposed through cygnus.
    /// </summary>
    public class RegisteredOutgoingEndpoint
    {
        /// <summary>
        /// The type of the interface that has been registered.
        /// </summary>
        public CygnusInterface Interface;

        /// <summary>
        /// The header of the CLI messages that are going to be sent out
        /// </summary>
        public AhsokaMessageHeader CLIHeader;

        /// <summary>
        /// The endpoint
        /// </summary>
        public ICygnusOutgoingEndpoints Endpoint;
    }
    private Dictionary<CygnusInterface, RegisteredOutgoingEndpoint> OutgoingEndpoints = new();

    /// <summary>
    /// Can Service Constructor accepting a customer Service Configuration
    /// </summary>
    public CygnusServerService()
    {
    }

    // TODO: BMG (Nov. 03, 2023) Figure out how we are going to be setting the inherent constants that were present in
    // the C version of the Reach server.
    /// <summary>
    /// 
    /// </summary>
    /// <param name="interfaceType"></param>
    /// <param name="endpoint"></param>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    /// <exception cref="Exception"></exception>
    public object Request(CygnusInterface interfaceType, ICygnusOutgoingEndpoints endpoint, AhsokaMessageHeader messageHeader, object message)
    {
        if (!DataValuesReady)
            GetDataValues();

        if (!OutgoingEndpoints.ContainsKey(interfaceType))
            OutgoingEndpoints[interfaceType] = new()
            {
                Interface = interfaceType,
                Endpoint = endpoint,
            };

        // NOTE: BMG (Nov. 03, 2023) I'm expecting to receive an object array for all messages because some messages are
        // going to return a number of responses instead of just one.
        switch (messageHeader.TransportId.IntToEnum<ReachMessageTypes>())
        {
            case ReachMessageTypes.Ping:
                if (message is PingRequest pingRequest)
                    return ProcessPing(interfaceType, messageHeader, pingRequest);
                break;

            case ReachMessageTypes.GetDeviceInfo:
                if (message is DeviceInfoRequest deviceInfoRequest)
                    return ProcessGetDeviceInfo(interfaceType, messageHeader, deviceInfoRequest);
                break;

            case ReachMessageTypes.DiscoverParameters:
                if (message is ParameterInfoRequest discoverParametersRequest)
                    return ProcessDiscoverParameters(interfaceType, messageHeader, discoverParametersRequest);
                break;

            case ReachMessageTypes.DiscoverParamEx:
                if (message is ParameterInfoRequest discoverParametersExRequest)
                    return ProcessDiscoverParametersEx(interfaceType, messageHeader, discoverParametersExRequest);
                break;

            case ReachMessageTypes.ReadParameters:
                if (message is ParameterRead parameterReadRequest)
                    return ProcessReadParameters(interfaceType, messageHeader, parameterReadRequest);
                break;

            case ReachMessageTypes.WriteParameters:
                if (message is ParameterWrite parameterWriteRequest)
                    return ProcessWriteParameters(interfaceType, messageHeader, parameterWriteRequest);
                break;

            case ReachMessageTypes.DiscoverFiles:
                if (message is DiscoverFiles discoverFilesRequest)
                    return ProcessDiscoverFiles(interfaceType, messageHeader, discoverFilesRequest);
                break;

            case ReachMessageTypes.TransferInit:
                if (message is FileTransferInit fileTransferInitRequest)
                    return ProcessTransferInit(interfaceType, messageHeader, fileTransferInitRequest);
                break;

            case ReachMessageTypes.TransferData:
                if (message is FileTransferData fileTransferDataRequest)
                    return ProcessTransferData(interfaceType, messageHeader, fileTransferDataRequest);
                break;

            case ReachMessageTypes.TransferDataNotification:
                if (message is FileTransferDataNotification fileTransferDataNotificationRequest)
                    return ProcessTransferDataNotification(interfaceType, messageHeader, fileTransferDataNotificationRequest);
                break;

            case ReachMessageTypes.DiscoverStreams:
                return ProcessDiscoverStreams(interfaceType, messageHeader, new object());

            case ReachMessageTypes.OpenStream:
                return ProcessOpenStreams(interfaceType, messageHeader, new object());

            case ReachMessageTypes.CloseStream:
                return ProcessCloseStreams(interfaceType, messageHeader, new object());

            case ReachMessageTypes.StreamDataNotification:
                return ProcessStreamDataNotification(interfaceType, messageHeader, new object());

            case ReachMessageTypes.DiscoverCommands:
                if (message is DiscoverCommands discoverCommandsRequest)
                    return ProcessDiscoverCommands(interfaceType, messageHeader, discoverCommandsRequest);
                break;

            case ReachMessageTypes.SendCommand:
                if (message is SendCommand sendCommandRequest)
                    return ProcessSendCommand(interfaceType, messageHeader, sendCommandRequest);
                break;

            case ReachMessageTypes.CliNotification:
                if (message is CLIData cliDataRequest)
                {
                    ProcessCliData(interfaceType, messageHeader, cliDataRequest);
                    return new CLIData();
                }
                break;

            default:
                break;
        }
        // TODO: BMG (Nov. 02, 2023) We are going to be throwing an error here to indicate that we received an
        // unsupported message type.
        // cr_report_error(CR_ERROR_NOT_IMPLEMENTED, "Unhandled message type %d.", message_type);
        // LOG_ERROR("Unhandled message type %d.", message_type);
        // rval = CR_ERROR_NOT_IMPLEMENTED;
        throw new Exception("Unsupported message header received.");
    }

    private UInt32 servicesAvailable = 0;
    private void GetDataValues()
    {
        GetParameterValueKeys();

        GetFileValueKeys();

        GetStreamValueKeys();

        GetCommandValueKeys();

        GetCLIValueKeys();

        DataValuesReady = true;
    }

    // TODO: BMG (Nov. 03, 2023) The functions below use the NotImplementedException as a way of indicating that they
    // are not implemented. Once we do the first development pass through these functions we need to change this to use
    // the appropriate error packet in response to requests. Another thing to remember here is that we can organize
    // these functions into their own files and make sure they are encapsulated in a partial class wrapper for this same
    // class. Might not be immediately necessary, but with enough commands we support we are definitely going to be
    // needing to do it.
    // TODO: BMG (Nov. 03, 2023) Another function needs to be added to this server which is the error message function.
    // I'm not 100% sure where this should be handled, but it definitely should exist to standardized the error message
    // process.
    private PingResponse ProcessPing(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, PingRequest request)
    {
        PingResponse response = new PingResponse();
        response.EchoData = request.EchoData;

        return response;
    }

    private DeviceInfoResponse ProcessGetDeviceInfo(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, DeviceInfoRequest deviceInfoRequest)
    {
        // TODO: BMG (Nov. 03, 2023) Obviously this is going to have to be updated to actually produce relevant
        // information, likely from the global data store that is currently being implemented.
        DeviceInfoResponse response = new DeviceInfoResponse();
        response.ProtocolVersion        = 5;

        SystemService.GetSystemInfo(out string appName, out string appDescription, out string appVersion, out string sdkVersion);

        response.DeviceName             = appName;
        response.Manufacturer           = "Enovation Controls";
        response.DeviceDescription      = appDescription;
        response.FirmwareVersion        = sdkVersion;
        // response.MaxMessageSize         = 244; // This is the maximum size for a BLE packet.
        response.Services               = servicesAvailable;
        response.ParameterMetadataHash  = ParameterValueKeysChecksum;
        response.ApplicationIdentifier  = Encoding.UTF8.GetBytes(appVersion);
        // response.Endpoints              = 0;
        // response.ParameterBufferCount   = CygnusBufferSizes.ParameterBufferCount;
        // response.NumMediumStructsInMsg  = CygnusBufferSizes.NumMediumStructsInMsg;
        // response.BigDataBufferSize      = CygnusBufferSizes.BigDataBufferSize;

        List<byte> sizeStruct = new();
        if (BitConverter.IsLittleEndian)
        {
            sizeStruct.AddRange(BitConverter.GetBytes((UInt16)CygnusBufferSizes.MaxMessageSize));
            sizeStruct.AddRange(BitConverter.GetBytes((UInt16)CygnusBufferSizes.BigDataBufferSize));
        }
        else
        {
            sizeStruct.AddRange(BitConverter.GetBytes((UInt16)CygnusBufferSizes.MaxMessageSize).Reverse());
            sizeStruct.AddRange(BitConverter.GetBytes((UInt16)CygnusBufferSizes.BigDataBufferSize).Reverse());
        }
        sizeStruct.Add((byte)CygnusBufferSizes.ParameterBufferCount);
        sizeStruct.Add((byte)CygnusBufferSizes.NumMediumStructsInMsg);
        sizeStruct.Add((byte)CygnusBufferSizes.DeviceInfoLen);
        sizeStruct.Add((byte)CygnusBufferSizes.LongStringLen);
        sizeStruct.Add((byte)CygnusBufferSizes.CountParamIds);
        sizeStruct.Add((byte)CygnusBufferSizes.MediumStringLen);
        sizeStruct.Add((byte)CygnusBufferSizes.ShortStringLen);
        sizeStruct.Add((byte)CygnusBufferSizes.ParamInfoEnumCount);
        sizeStruct.Add((byte)CygnusBufferSizes.ServicesCount);
        sizeStruct.Add((byte)CygnusBufferSizes.PiEnumCount);
        sizeStruct.Add((byte)CygnusBufferSizes.NumCommandsInResponse);
        sizeStruct.Add((byte)CygnusBufferSizes.CountParamDescInResponse);

        response.SizesStruct = sizeStruct.ToArray();

        return response;
    }
}
