using Ahsoka.Dispatch;
using System;

namespace Ahsoka.Demo.Storyboard;

internal class Simulation
{
    public static Simulation Default { get; private set; } = new();
    public ThermostatUpdate ThermostatUpdate { get; } = new();
    short currentTemp, step;

    internal void Init()
    {
        // Register Callback From Time Dispatch
        Dispatcher.Default.RegisterTimerCallback(HandleTimeoutMessage, TimeSpan.FromSeconds(1));

        // Setup Temperature State
        ThermostatUpdate.TargetTemperature = 71;
        ThermostatUpdate.CurrentTemperature = 72;
        ThermostatUpdate.Ac = 1;
        ThermostatUpdate.Fan = 1;
        ThermostatUpdate.Timer = 1;
        ThermostatUpdate.Units = 0;

        currentTemp = ThermostatUpdate.TargetTemperature;
        step = 1;

    }

    public void HandleTimeoutMessage(object model, EventArgs args)
    {
        currentTemp += step;

        if (currentTemp > 80)
        {
            ThermostatUpdate.Ac = 0;
            step = -1;
        }
        else if (currentTemp < 60)
        {
            ThermostatUpdate.Ac = 1;
            step = 1;
        }

        ThermostatUpdate.CurrentTemperature = currentTemp;
        ThermostatUpdate.UpdateStoryboard();
    }
}

