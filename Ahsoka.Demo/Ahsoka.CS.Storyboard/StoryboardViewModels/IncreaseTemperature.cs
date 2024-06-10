namespace Ahsoka.Demo.Storyboard;
public partial class IncreaseTemperature
{
    public override void OnUpdateModel(string target)
    {
        Simulation.Default.ThermostatUpdate.TargetTemperature += (short)Num;
        base.OnUpdateModel(target);
    }
}
