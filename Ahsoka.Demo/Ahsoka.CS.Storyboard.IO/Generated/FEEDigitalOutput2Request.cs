using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("FEE_DigitalOutput2Request", "0x20")]
public partial class FEEDigitalOutput2Request : ApplicationViewModelBase
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
