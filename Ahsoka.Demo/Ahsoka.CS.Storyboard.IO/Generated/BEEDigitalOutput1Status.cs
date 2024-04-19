using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_DigitalOutput1Status", "0x40")]
public partial class BEEDigitalOutput1Status : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private Int32 _state;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "state")]
	public Int32 State 
	{
		get { return _state;}
		set { OnSetValue<Int32>(ref _state, value);} 
	}

	#endregion
}
