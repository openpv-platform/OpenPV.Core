namespace Ahsoka.Application.Storyboard;
public partial class BEEPartNumber
{
    public override void OnUpdateStoryboard()
    {
        PartNumber = ApplicationViewModelRoot.Default.SystemClient.GetHardwareInformation().PartNumber;
    }
}
