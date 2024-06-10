namespace Ahsoka.Application.Storyboard;
public partial class BEEDeviceId
{
    public override void OnUpdateStoryboard()
    {
        DeviceId = ApplicationViewModelRoot.Default.SystemClient.GetHardwareInformation().DeviceId;
    }
}
