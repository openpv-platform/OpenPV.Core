#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO.Platform;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Azure;
using System;
using System.IO;

namespace Ahsoka.Services.IO;

[AhsokaService(Name)]
public class IOService : AhsokaTCPServiceBase<IOMessageTypes.Ids>
{
    #region Fields
    public const string Name = "IOService";
    private readonly IOServiceImplementationBase _IOImplementationBase;
    // private IOServiceSettings _settings;
    #endregion

    #region Events
    #endregion

    #region Methods
    internal static IOServiceImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WindowsServiceImplementation(),
            PlatformFamily.OpenViewLinux => new STServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    public IOService() : this(ConfigurationLoader.GetServiceConfig(Name)) { }

    public IOService(ServiceConfiguration config) : base(config, new IOServiceMessages())
    {
        _IOImplementationBase = GetInterop();
        _IOImplementationBase.Init(this, SystemInfo.HardwareInfo.IOInfo);

        // Disable Service if Running Codesys
        string currentAppPath = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);

        if (Environment.ProcessPath.ToLower().StartsWith(currentAppPath.ToLower()))
        {
            string appTypeFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationType);
            if (File.Exists(appTypeFile) && File.ReadAllText(appTypeFile) == ApplicationType.CODESYS.ToString())
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "ERROR - IO Service is not Supported on CODESYS Targets");
                throw new ApplicationException("ERROR - IO Service is not Supported on CODESYS Targets");
            }
        }
    }

    protected override void OnEndPointDisconnected()
    {
        base.OnEndPointDisconnected();
        _IOImplementationBase.StopPolling();
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<IOMessageTypes.Ids>())
        {
            case IOMessageTypes.Ids.SetPollInterval:
                HandleSetPollInterval(messageHeader, message as PollingInterval);
                break;

            case IOMessageTypes.Ids.GetPollInterval:
                HandleGetPollInterval(messageHeader);
                break;

            case IOMessageTypes.Ids.RetrieveDigitalOutputs:
                HandleRetrieveDigitalOutputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.RetrieveDigitalInputs:
                HandleRetrieveDigitalInputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.RetrieveAnalogOutputs:
                HandleRetrieveAnalogOutputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.RetrieveAnalogInputs:
                HandleRetrieveAnalogInputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.SetDigitalOutput:
                HandelSetDigitalOut(messageHeader, message as DigitalOutput);
                break;

            case IOMessageTypes.Ids.SetAnalogOutput:
                HandelSetAnalogOut(messageHeader, message as AnalogOutput);
                break;

            case IOMessageTypes.Ids.GetAnalogInput:
                HandleGetAnalogIn(messageHeader, message as AnalogInput);
                break;

            case IOMessageTypes.Ids.GetDigitalInput:
                HandleGetDigitalIn(messageHeader, message as DigitalInput);
                break;

            // case IOMessageTypes.Ids.SetHardwareConfig:
            //     HandleSetHardwareConfig(messageHeader, message as HardwareConfig);
            //     break;

            // case IOMessageTypes.Ids.GetHardwareConfig:
            //     HandleGetHardwareConfig(messageHeader, message);
            //     break;

            default:
                break;
        }
    }

    private void HandleGetPollInterval(AhsokaMessageHeader messageHeader)
    {
        var response = _IOImplementationBase.GetPollingInterval();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleSetPollInterval(AhsokaMessageHeader messageHeader, PollingInterval message)
    {
        _IOImplementationBase.SetPollingInterval(message);
        SendMessageInternal(messageHeader);

    }

    private void HandleRetrieveDigitalOutputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveDigitalOutputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleRetrieveDigitalInputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveDigitalInputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleRetrieveAnalogInputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveAnalogInputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleRetrieveAnalogOutputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveAnalogOutputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleGetDigitalIn(AhsokaMessageHeader messageHeader, DigitalInput d)
    {
        var response = _IOImplementationBase.GetDigitalInput(d);
        SendMessageInternal(messageHeader, response);
    }

    private void HandleGetAnalogIn(AhsokaMessageHeader messageHeader, AnalogInput a)
    {
        var response = _IOImplementationBase.GetAnalogInput(a);
        SendMessageInternal(messageHeader, response);
    }

    private void HandelSetDigitalOut(AhsokaMessageHeader messageHeader, DigitalOutput d)
    {
        var response = _IOImplementationBase.SetDigitalOut(d);
        SendMessageInternal(messageHeader, response);
    }

    private void HandelSetAnalogOut(AhsokaMessageHeader messageHeader, AnalogOutput a)
    {
        var response = _IOImplementationBase.SetAnalogOut(a);
        SendMessageInternal(messageHeader, response);
    }

    #endregion
}
