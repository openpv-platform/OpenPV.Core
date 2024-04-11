using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("view_network", "0x40")]
public partial class ViewNetwork : DemoViewModelBase
{
	#region Auto Generated Fields
	private Int32 _signal_level;
	private Byte _saved_profile;
	private string _ssid;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "signal_level")]
	public Int32 SignalLevel 
	{
		get { return _signal_level;}
		set { OnSetValue<Int32>(ref _signal_level, value);} 
	}

	[SBField(3, 1, "saved_profile")]
	public Byte SavedProfile 
	{
		get { return _saved_profile;}
		set { OnSetValue<Byte>(ref _saved_profile, value);} 
	}

	[SBField(5, 0, "ssid")]
	public string Ssid 
	{
		get { return _ssid;}
		set { OnSetObject<string>(ref _ssid, value);} 
	}

	#endregion
}
