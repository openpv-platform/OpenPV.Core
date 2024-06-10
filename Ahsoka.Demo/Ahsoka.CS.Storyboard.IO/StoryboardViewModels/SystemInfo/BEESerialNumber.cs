namespace Ahsoka.Application.Storyboard;
public partial class BEESerialNumber
{
    public override void OnUpdateStoryboard()
    {
        SerialNumber = ApplicationViewModelRoot.Default.SystemClient.GetHardwareInformation().SerialNumber;
    }
}
