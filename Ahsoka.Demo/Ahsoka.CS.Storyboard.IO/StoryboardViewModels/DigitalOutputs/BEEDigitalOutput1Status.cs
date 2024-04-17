using Ahsoka.Services.IO;
using Ahsoka.Utility;

namespace Ahsoka.Application.Storyboard;

public partial class BEEDigitalOutput1Status
{
    private bool Initialized = false;

    public void Update(PinState requestedState, SetOutputResponse response)
    {
        //Mark as initialized
        Initialized = true;

        //Error case
        if (response == null)
        {
            State = -1;
        }

        //Updates
        else if (response.Ret == ReturnCode.Success)
        {
            State = (int)requestedState;
        }

        //Update UI
        UpdateStoryboard();

    }

    public override void OnUpdateStoryboard()
    {
        //If not initialized
        if (!Initialized)
        {
            State = -1;
            return;
        }

    }

}