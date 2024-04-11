using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("change_brightness", "0x20")]
public partial class ChangeBrightness : DemoViewModelBase
{
	#region Auto Generated Fields
	private UInt16 _value;
	private Byte _increase;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "value")]
	public UInt16 Value 
	{
		get { return _value;}
		set { OnSetValue<UInt16>(ref _value, value);} 
	}

	[SBField(3, 1, "increase")]
	public Byte Increase 
	{
		get { return _increase;}
		set { OnSetValue<Byte>(ref _increase, value);} 
	}

	#endregion
}
