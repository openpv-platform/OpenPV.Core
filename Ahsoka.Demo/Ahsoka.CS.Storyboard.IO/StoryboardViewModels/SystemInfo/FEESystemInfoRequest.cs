namespace Ahsoka.Application.Storyboard;
public partial class FEESystemInfoRequest
{

    public override void OnUpdateModel(string target)
    {
        ApplicationViewModelRoot.Default.TouchHardwareVersionModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.TouchFirmwareVersionModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.BSPVersionModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.ApplicationVersionModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.SDKVersionModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.InstallerVersionModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.PartNumberModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.SerialNumberModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.DeviceIdModel?.UpdateStoryboard();
        ApplicationViewModelRoot.Default.ApplicationNameModel?.UpdateStoryboard();
    }
}
