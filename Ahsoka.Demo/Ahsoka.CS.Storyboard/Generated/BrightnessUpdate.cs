using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("brightness_update", "0x40")]
public partial class BrightnessUpdate : DemoViewModelBase
{
	#region Auto Generated Fields
	private UInt16 _percentage;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "percentage")]
	public UInt16 Percentage 
	{
		get { return _percentage;}
		set { OnSetValue<UInt16>(ref _percentage, value);} 
	}

	#endregion
}
