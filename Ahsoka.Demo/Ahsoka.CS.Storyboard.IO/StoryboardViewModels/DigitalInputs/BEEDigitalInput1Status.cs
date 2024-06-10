using Ahsoka.Services.IO;
using System.Linq;

namespace Ahsoka.Application.Storyboard;

public partial class BEEDigitalInput1Status
{
    static readonly DigitalInput input = ApplicationViewModelRoot.Default.IOClient.RequestDigitalInputs().DigitalInputs.FirstOrDefault(input => input.Pin == 1);

    public bool Update()
    {
        //If there is not input, bail
        if (input == null)
        {
            return false;
        }

        //Get the current value of the pin
        float current = (float)ApplicationViewModelRoot.Default.IOClient.GetDigitalInput(input)?.Value;

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