using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_BrightnessStatus", "0x40")]
public partial class BEEBrightnessStatus : ApplicationViewModelBase
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
