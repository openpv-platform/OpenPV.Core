namespace Ahsoka.Demo.Storyboard;

public partial class Shutdown
{
    public override void OnUpdateModel(string target)
    {
        base.OnUpdateModel(target);
        DemoViewModelRoot.Default.StopModel();
    }
}
