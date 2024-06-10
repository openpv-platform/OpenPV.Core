using Ahsoka.ServiceFramework;
using Cygnus.Reach;

namespace Ahsoka.Services.Cygnus;

/// <summary>
/// 
/// </summary>
public class CygnusServerServiceMessages : AhsokaMessagesBase
{
    /// <summary>
    /// 
    /// </summary>
    public CygnusServerServiceMessages()
    {
        this.RegisterServiceRequest(ReachMessageTypes.Ping,                     typeof(PingRequest), typeof(PingResponse), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.GetDeviceInfo,            typeof(DeviceInfoRequest), typeof(DeviceInfoResponse), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.DiscoverParameters,       typeof(ParameterInfoRequest), typeof(ParameterInfoResponse), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.DiscoverParamEx,          typeof(ParameterInfoRequest), typeof(ParamExInfoResponse), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.ReadParameters,           typeof(ParameterRead), typeof(ParameterReadResult), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.WriteParameters,          typeof(ParameterWrite), typeof(ParameterWriteResult), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.DiscoverFiles,            typeof(DiscoverFiles), typeof(DiscoverFilesReply), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.TransferInit,             typeof(FileTransferInit), typeof(FileTransferInitReply), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.TransferData,             typeof(FileTransferData), typeof(FileTransferDataNotification), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.TransferDataNotification, typeof(FileTransferDataNotification), typeof(FileTransferData), false, false);
        // this.RegisterServiceRequest(ReachMessageTypes.DiscoverStreams,          typeof(), typeof(),     false, false);
        // this.RegisterServiceRequest(ReachMessageTypes.OpenStream,               typeof(), typeof(),     false, false);
        // this.RegisterServiceRequest(ReachMessageTypes.CloseStream,              typeof(), typeof(),     false, false);
        // this.RegisterServiceRequest(ReachMessageTypes.StreamDataNotification,   typeof(), typeof(),     false, false);
        this.RegisterServiceRequest(ReachMessageTypes.DiscoverCommands,         typeof(DiscoverCommands), typeof(DiscoverCommandsResult), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.SendCommand,              typeof(SendCommand), typeof(SendCommandResult), false, false);
        this.RegisterServiceRequest(ReachMessageTypes.CliNotification,          typeof(CLIData), typeof(CLIData), false, false);
    }
}

