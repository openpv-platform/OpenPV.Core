namespace Ahsoka.Application.Storyboard;
public partial class BEEApplicationName
{
    public override void OnUpdateStoryboard()
    {
        ApplicationName = ApplicationViewModelRoot.Default.SystemClient.GetHardwareInformation().ApplicationName;
    }
}
