namespace Ahsoka.Demo.Storyboard;
public partial class ViewNetwork
{
    public override void OnUpdateStoryboard()
    {
        var selectedNetwork = DemoViewModelRoot.Default.WiFiNetworkManager.SelectedNetwork;
        Ssid = selectedNetwork.Ssid;
        SignalLevel = selectedNetwork.SignalLevel;
        if (DemoViewModelRoot.Default.WiFiNetworkManager.SelectedNetworkProfile != null)
            SavedProfile = 1;
        else
            SavedProfile = 0;

        base.OnUpdateStoryboard();
    }
}
