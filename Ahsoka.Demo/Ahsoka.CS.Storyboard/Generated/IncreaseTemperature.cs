using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("increase_temperature", "0x20")]
public partial class IncreaseTemperature : DemoViewModelBase
{
	#region Auto Generated Fields
	private UInt16 _num;
	private UInt16 _unit;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "num")]
	public UInt16 Num 
	{
		get { return _num;}
		set { OnSetValue<UInt16>(ref _num, value);} 
	}

	[SBField(3, 1, "unit")]
	public UInt16 Unit 
	{
		get { return _unit;}
		set { OnSetValue<UInt16>(ref _unit, value);} 
	}

	#endregion
}
