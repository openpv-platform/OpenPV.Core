using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("FEE_BrightnessRequest", "0x20")]
public partial class FEEBrightnessRequest : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private Byte _percent;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "percent")]
	public Byte Percent 
	{
		get { return _percent;}
		set { OnSetValue<Byte>(ref _percent, value);} 
	}

	#endregion
}
