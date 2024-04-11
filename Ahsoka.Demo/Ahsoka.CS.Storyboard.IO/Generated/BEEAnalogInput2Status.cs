using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_AnalogInput2Status", "0x40")]
public partial class BEEAnalogInput2Status : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private float _value;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "value")]
	public float Value 
	{
		get { return _value;}
		set { OnSetValue<float>(ref _value, value);} 
	}

	#endregion
}
