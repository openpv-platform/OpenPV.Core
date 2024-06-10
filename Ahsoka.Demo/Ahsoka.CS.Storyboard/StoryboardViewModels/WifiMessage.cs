namespace Ahsoka.Demo.Storyboard;
public partial class WifiMessage
{
    public override void OnUpdateModel(string target)
    {
        switch (Action)
        {
            case 0:
                DemoViewModelRoot.Default.WiFiNetworkManager.ConnectNetwork(Data);
                break;
            case 1:
                DemoViewModelRoot.Default.WiFiNetworkManager.MoveUp();
                break;
            case 2:
                DemoViewModelRoot.Default.WiFiNetworkManager.MoveDown();
                break;
            case 3:
                DemoViewModelRoot.Default.WiFiNetworkManager.DiscoverNetworks();
                break;
            case 4:
                DemoViewModelRoot.Default.WiFiNetworkManager.Disconnect();
                break;
            default:
                break;
        }
        base.OnUpdateModel(target);
    }
}
