using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Demo.Storyboard;

[SBMessageEventName("connection_status", "0x40")]
public partial class ConnectionStatus : DemoViewModelBase
{
	#region Auto Generated Fields
	private UInt32 _signal_level;
	private string _ssid;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "signal_level")]
	public UInt32 SignalLevel 
	{
		get { return _signal_level;}
		set { OnSetValue<UInt32>(ref _signal_level, value);} 
	}

	[SBField(3, 0, "ssid")]
	public string Ssid 
	{
		get { return _ssid;}
		set { OnSetObject<string>(ref _ssid, value);} 
	}

	#endregion
}
