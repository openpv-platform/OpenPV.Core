using Ahsoka.System.Hardware;
using System.Diagnostics.CodeAnalysis;


namespace Ahsoka.Services.IO.Platform;

[ExcludeFromCodeCoverage]
internal class STServiceImplementation : IOServiceImplementationBase
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
        analogInputImplementation = new RCDAnalogInput();
        analogOutputImplementation = new RCDAnalogOutput();
        digitalInputImplementation = new RCDDigitalInput();
        digitalOutputImplementation = new RCDDigitalOutput();
    }

    #endregion
}