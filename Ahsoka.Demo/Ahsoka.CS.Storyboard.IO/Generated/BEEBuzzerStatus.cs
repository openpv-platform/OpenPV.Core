using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_BuzzerStatus", "0x40")]
public partial class BEEBuzzerStatus : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private UInt32 _frequency;
	private Byte _volumePercent;
	private Byte _enable;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "frequency")]
	public UInt32 Frequency 
	{
		get { return _frequency;}
		set { OnSetValue<UInt32>(ref _frequency, value);} 
	}

	[SBField(3, 1, "volumePercent")]
	public Byte VolumePercent 
	{
		get { return _volumePercent;}
		set { OnSetValue<Byte>(ref _volumePercent, value);} 
	}

	[SBField(5, 1, "enable")]
	public Byte Enable 
	{
		get { return _enable;}
		set { OnSetValue<Byte>(ref _enable, value);} 
	}

	#endregion
}
