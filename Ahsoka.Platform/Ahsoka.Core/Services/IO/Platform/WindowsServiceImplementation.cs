using Ahsoka.System.Hardware;


namespace Ahsoka.Services.IO.Platform;

internal class WindowsServiceImplementation : IOServiceImplementationBase
{
    #region Fields
    #endregion

    #region Methods
    protected override void OnHandleInit(IOHardwareInfo IOInfo,
         out IAnalogInputImplementation analogInputImplementation,
         out IAnalogOutputImplementation analogOutputImplementation,
         out IDigitalInputImplementation digitalInputImplementation,
         out IDigitalOutputImplementation digitalOutputImplementation)
    {
        analogInputImplementation = new WinAnalogInput();
        analogOutputImplementation = new WinAnalogOutput();
        digitalInputImplementation = new WinDigitalInput();
        digitalOutputImplementation = new WinDigitalOutput();
    }

    #endregion
}
