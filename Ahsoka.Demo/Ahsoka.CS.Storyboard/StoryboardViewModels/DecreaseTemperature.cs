namespace Ahsoka.Demo.Storyboard;

public partial class DecreaseTemperature
{
    public override void OnUpdateModel(string target)
    {
        Simulation.Default.ThermostatUpdate.TargetTemperature -= (short)Num;
        base.OnUpdateModel(target);
    }
}
