using System;
using System.Collections.Generic;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Cygnus.Reach;
using CygnusCommandInfo = Cygnus.Reach.CommandInfo;

namespace Ahsoka.Services.Cygnus;

/// <summary>
/// A TCP client for Ahsoka serial socket communication with the Cygnus server
/// </summary>
public class TCPCygnusServerClient : AhsokaClientBase<ReachMessageTypes, TCPClientSerialSocket>
{
    /// <summary>
    /// Default Constructor
    /// </summary>
    public TCPCygnusServerClient()
        : base(ConfigurationLoader.GetServiceConfig(CygnusServerInterfaceService<TCPClientSerialSocket>.Name), new CygnusServerServiceMessages())
    {
    }

    /// <summary>
    /// Creates a Default Service Instance for In Proc and
    /// automatic service construction.
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        throw new NotImplementedException("This test client is never going to have an explicit server that corresponds to it.");
    }

    /// <summary>
    /// A manual way of prompting the underlying socket to connect to the server.
    /// </summary>
    public void Connect()
    {
        this.Socket.Connect();
    }

    /// <summary>
    /// A manual way of prompting the underlying socket to disconnect to the server.
    /// </summary>
    public void Disconnect()
    {
        this.Socket.Disconnect();
    }

    /// <summary>
    /// Pinging the cygnus server
    /// </summary>
    /// <param name="echoData"></param>
    public PingResponse Ping(byte[] echoData)
    {
        PingRequest request = new PingRequest();
        request.EchoData = echoData;

        return SendNotificationWithResponse<PingResponse>(ReachMessageTypes.Ping, request);
    }

    /// <summary>
    /// Get the device info from the cygnus server
    /// </summary>
    public DeviceInfoResponse GetDeviceInfo()
    {
        DeviceInfoRequest request = new DeviceInfoRequest();

        return SendNotificationWithResponse<DeviceInfoResponse>(ReachMessageTypes.GetDeviceInfo, request);
    }

    /// <summary>
    /// Discover all the parameters available on the cygnus server
    /// </summary>
    public List<ParameterInfo> DiscoverParameters()
    {
        return DiscoverParameters(Array.Empty<uint>());
    }

    /// <summary>
    /// Discover specific parameters available on the cygnus server
    /// </summary>
    /// <param name="requestValues"></param>
    public List<ParameterInfo> DiscoverParameters(uint[] requestValues)
    {
        ParameterInfoRequest request = new ParameterInfoRequest();
        request.ParameterIds = requestValues;

        ParameterInfoResponse parameterInfoResponse =
            SendNotificationWithResponse<ParameterInfoResponse>(ReachMessageTypes.DiscoverParameters);

        return parameterInfoResponse.ParameterInfos;
    }

    /// <summary>
    /// Read specific parameter values from the cygnus server
    /// </summary>
    /// <param name="requestValues"></param>
    /// <param name="parameterKey"></param>
    public List<ParameterValue> ReadParameters(List<uint> requestValues, uint parameterKey = 0)
    {
        ParameterRead request = new ParameterRead();
        request.ParameterIds = requestValues.ToArray();
        if (parameterKey != 0)
            request.ParameterKey = parameterKey;

        ParameterReadResult parameterReadResponse =
            SendNotificationWithResponse<ParameterReadResult>(ReachMessageTypes.ReadParameters, request);

        return parameterReadResponse.Values;
    }

    /// <summary>
    /// Read specific parameter values from the cygnus server
    /// </summary>
    /// <param name="requestValues"></param>
    /// <param name="parameterKey"></param>
    public Int32 WriteParameters(List<ParameterValue> requestValues, uint parameterKey = 0)
    {
        ParameterWrite request = new ParameterWrite();
        request.Values.AddRange(requestValues);
        if (parameterKey != 0)
            request.ParameterKey = parameterKey;

        ParameterWriteResult parameterWriteResponse =
            SendNotificationWithResponse<ParameterWriteResult>(ReachMessageTypes.WriteParameters, request);

        return parameterWriteResponse.Result;
    }

    /// <summary>
    /// Discover all the commands available on the Cygnus server.
    /// </summary>
    public DiscoverCommandsResult DiscoverCommands()
    {
        DiscoverCommands request = new();

        return SendNotificationWithResponse<DiscoverCommandsResult>(ReachMessageTypes.DiscoverCommands, request);
    }

    /// <summary>
    /// Discover all the commands available on the Cygnus server.
    /// </summary>
    public SendCommandResult SendCommand(CygnusCommandInfo command)
    {
        SendCommand request = new()
        {
            CommandId = command.Id
        };

        return SendNotificationWithResponse<SendCommandResult>(ReachMessageTypes.SendCommand, request);
    }

    // public CLIData RunCommand(CygnusCommandInfo command)
    // {
    //     SendCommand request = new()
    //     {
    //         CommandId = command.Id
    //     };

    //     return SendNotificationWithResponse<SendCommandResult>(ReachMessageTypes.comm, request);
    // }
}
