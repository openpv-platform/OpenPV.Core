using Ahsoka.Services.IO;
using System.Linq;

namespace Ahsoka.Application.Storyboard;

public partial class BEEAnalogInput2Status
{
    static readonly AnalogInput input = ApplicationViewModelRoot.Default.IOClient.RequestAnalogInputs().AnalogInputs.FirstOrDefault(input => input.Pin == 2);

    public bool Update()
    {
        //If there is not input, bail
        if (input == null)
        {
            return false;
        }

        //Get the current value of the pin
        float current = (float)ApplicationViewModelRoot.Default.IOClient.GetAnalogInput(input)?.Value;

        //If the value has changed, update the value and mark that it changed
        bool HasChanges = false;
        if (Value != current)
        {
            Value = current;
            HasChanges = true;
        }

        return HasChanges;
    }

}