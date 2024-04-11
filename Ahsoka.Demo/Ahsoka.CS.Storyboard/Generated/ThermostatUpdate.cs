using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("thermostat_update", "0x40")]
public partial class ThermostatUpdate : DemoViewModelBase
{
	#region Auto Generated Fields
	private Int16 _target_temperature;
	private Int16 _current_temperature;
	private Byte _ac;
	private Byte _fan;
	private Byte _timer;
	private Byte _units;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "target_temperature")]
	public Int16 TargetTemperature 
	{
		get { return _target_temperature;}
		set { OnSetValue<Int16>(ref _target_temperature, value);} 
	}

	[SBField(3, 1, "current_temperature")]
	public Int16 CurrentTemperature 
	{
		get { return _current_temperature;}
		set { OnSetValue<Int16>(ref _current_temperature, value);} 
	}

	[SBField(5, 1, "ac")]
	public Byte Ac 
	{
		get { return _ac;}
		set { OnSetValue<Byte>(ref _ac, value);} 
	}

	[SBField(7, 1, "fan")]
	public Byte Fan 
	{
		get { return _fan;}
		set { OnSetValue<Byte>(ref _fan, value);} 
	}

	[SBField(9, 1, "timer")]
	public Byte Timer 
	{
		get { return _timer;}
		set { OnSetValue<Byte>(ref _timer, value);} 
	}

	[SBField(11, 1, "units")]
	public Byte Units 
	{
		get { return _units;}
		set { OnSetValue<Byte>(ref _units, value);} 
	}

	#endregion
}
