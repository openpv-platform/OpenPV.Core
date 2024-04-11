using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_DigitalInput1Status", "0x40")]
public partial class BEEDigitalInput1Status : ApplicationViewModelBase
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
