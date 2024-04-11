using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("wifi_message", "0x20")]
public partial class WifiMessage : DemoViewModelBase
{
	#region Auto Generated Fields
	private UInt16 _action;
	private string _data;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "action")]
	public UInt16 Action 
	{
		get { return _action;}
		set { OnSetValue<UInt16>(ref _action, value);} 
	}

	[SBField(3, 0, "data")]
	public string Data 
	{
		get { return _data;}
		set { OnSetObject<string>(ref _data, value);} 
	}

	#endregion
}
