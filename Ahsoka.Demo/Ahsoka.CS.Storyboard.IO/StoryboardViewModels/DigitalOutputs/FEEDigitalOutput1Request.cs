using Ahsoka.Services.IO;
using Ahsoka.Utility;
using System.Linq;

namespace Ahsoka.Application.Storyboard;

public partial class FEEDigitalOutput1Request
{

    public static readonly DigitalOutput digOut = ApplicationViewModelRoot.Default.IOClient.RequestDigitalOutputs().DigitalOutputs.FirstOrDefault(digOut => digOut.Pin == 1);

    public void Init()
    {
        if (digOut == null)
        {
            return;
        }

        digOut.State = PinState.Low;
        SetOutputResponse response = ApplicationViewModelRoot.Default.IOClient.SetDigitalOut(digOut);
        ApplicationViewModelRoot.Default.DigitalOutput1.Update(PinState.Low, response);
    }

    public override void OnUpdateModel(string target)
    {

        //Convert to the appropriate pin state
        PinState pinState;
        if (State == 0)
        {
            pinState = PinState.Low;
        }
        else if (State > 0)
        {
            pinState = PinState.High;
        }
        else
        {
            return;
        }

        //Make request to model
        SetOutputResponse response;
        if (digOut != null)
        {
            digOut.State = pinState;
            response = ApplicationViewModelRoot.Default.IOClient.SetDigitalOut(digOut);
        }
        else
        {
            response = null;
        }
        ApplicationViewModelRoot.Default.DigitalOutput1.Update(pinState, response);

    }
}
