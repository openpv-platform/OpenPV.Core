using Ahsoka.ServiceFramework;

namespace Ahsoka.Application.Storyboard;
public partial class FEEShutdownRequest
{

    public override void OnUpdateModel(string target)
    {

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"C# Shutdown Request");

        //Make request to the model
        ApplicationViewModelRoot.Default.StopModel();

    }
}
