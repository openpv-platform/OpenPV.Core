using System;
using System.Collections.Generic;
using System.IO;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.System;
using Ahsoka.Utility;

using CygnusCommandInfo = Cygnus.Reach.CommandInfo;

namespace Ahsoka.Services.Cygnus;

[AhsokaService(Name)]
internal class CygnusService : AhsokaTCPServiceBase<CygnusMessageTypes.Ids>
{
    public const string Name = "CygnusService";

    // private readonly CygnusServerServiceClient LocalCygnusServerServiceClient = new CygnusServerServiceClient();
    private readonly DataServiceClient DataClient = new DataServiceClient();
    // private KeyInfo ReachCommandData;
    private int ReachCommandDataKeyId;

    public CygnusService()
        : this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public CygnusService(ServiceConfiguration config) :
        base(config, new CygnusServiceMessage())
    {
    }

    private Dictionary<CygnusInterface, IAhsokaServiceEndpoint> InterfaceServices = new();
    private void StartSocketTypeFromInterfaceType(CygnusInterface interfaceType)
    {
        InterfaceServices[interfaceType] = interfaceType switch
        {
            CygnusInterface.Ble             => new CygnusServerInterfaceService<BLEServiceSocket>(interfaceType),
            CygnusInterface.BLECygnus       => new CygnusServerInterfaceService<CygnusBLEServiceSocket>(interfaceType),
            CygnusInterface.Tcp             => new CygnusServerInterfaceService<TCPServiceSocket>(interfaceType),
            CygnusInterface.TCPSerial       => new CygnusServerInterfaceService<TCPServiceSerialSocket>(interfaceType),
            CygnusInterface.TCPCygnusSerial => new CygnusServerInterfaceService<CygnusTCPServiceSerialSocket>(interfaceType),
            CygnusInterface.Can             => throw new NotImplementedException("The CAN interface isn't implemented yet."),
            _ => throw new Exception("The interface type received isn't valid."),
        };

        AhsokaRuntime.CreateBuilder().AddServices(InterfaceServices[interfaceType]).StartLocalRuntime();
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<CygnusMessageTypes.Ids>())
        {
            case CygnusMessageTypes.Ids.EnableTransport:
                if (message is CygnusServiceEnableDisableTransports enableTransports)
                    EnableTransport(messageHeader, enableTransports);
                break;

            case CygnusMessageTypes.Ids.AddFile:
                if (message is CygnusServiceFileInfo fileInfo)
                    AddFile(messageHeader, fileInfo);
                break;

            // case CygnusMessageTypes.Ids.DefineEnum:
            //     if (message is )
            //         DefineEnum(messageHeader, fileInfo);
            //     break;

            case CygnusMessageTypes.Ids.AddCommand:
                if (message is CygnusServiceCommandInfo commandInfo)
                    AddCommand(messageHeader, commandInfo);
                break;

            case CygnusMessageTypes.Ids.EnableCli:
                EnableCLI(messageHeader);
                break;
        }
    }

    private void EnableTransport(AhsokaMessageHeader messageHeader, CygnusServiceEnableDisableTransports transport)
    {
        StartSocketTypeFromInterfaceType(transport.Interface);
        SendMessageInternal(messageHeader);
    }

    private void AddFile(AhsokaMessageHeader messageHeader, CygnusServiceFileInfo fileInfo)
    {
        using MemoryStream fileInfoStream = new();
        ProtoBuf.Serializer.Serialize(fileInfoStream, fileInfo);
        byte[] fileInfoByteArray = fileInfoStream.ToArray();

        UpdateDataServiceKey($"{CygnusServerService.ReachConfigFiles}{fileInfo.Name}", fileInfoByteArray);

        SendMessageInternal(messageHeader);
    }

    // private void DefineEnum(AhsokaMessageHeader messageHeader, CygnusServiceEnableDisableTransports fileInfo)
    // {
    //     SendMessageInternal(messageHeader);
    // }


    private void AddCommand(AhsokaMessageHeader messageHeader, CygnusServiceCommandInfo commandInfoName)
    {
        AvailableKeySearchInfo searchInfo = new()
        {
            KeyFilter = CygnusServerService.ReachConfigCommands,
        };
        CygnusCommandInfo cygnusCommandInfo = new()
        {
            Name = commandInfoName.Name,
            Id = (uint)DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: 1000).Keys.Count,
        };

        using MemoryStream commandInfoStream = new();
        ProtoBuf.Serializer.Serialize(commandInfoStream, cygnusCommandInfo);
        byte[] commandInfoByteArray = commandInfoStream.ToArray();

        string commandKeyString = $"{CygnusServerService.ReachConfigCommands}{commandInfoName.Name}";
        UpdateDataServiceKey(commandKeyString, commandInfoByteArray);

        // NOTE: BMG (Jan. 25, 2024) We are initializing the command data value to be an array of size 1, because a
        // serialized "SendCommand" protobuf structure with default values is of size 0, so if we initialize to an empty
        // array we could miss receiving the send command for command at index 0.
        if (ReachCommandDataKeyId <= 0)
            ReachCommandDataKeyId = UpdateDataServiceKey(CygnusServerService.ReachCommandData, new byte[1] { 0x00 }).ValueId;

        CygnusServiceCommandInfoResponse response = new();
        response.CommandInfo = cygnusCommandInfo;
        response.DataKeyId = ReachCommandDataKeyId;

        SendMessageInternal(messageHeader, response);
    }

    private void EnableCLI(AhsokaMessageHeader messageHeader)
    {
        AvailableKeySearchInfo searchInfo = new()
        {
            KeyFilter = CygnusServerService.ReachCLIData,
        };

        if (DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: 1000).Keys.Count != 0)
        {
            CygnusServiceEnableCLIResponse errorResponse = new();
            errorResponse.DataKeyId = -1;
            SendMessageInternal(messageHeader, errorResponse);
            return;
        }

        CygnusServiceEnableCLIResponse response = new();
        response.DataKeyId = UpdateDataServiceKey(CygnusServerService.ReachCLIData, Array.Empty<byte>()).ValueId;

        SendMessageInternal(messageHeader, response);
    }
}
