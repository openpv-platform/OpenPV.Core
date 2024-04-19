using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_TouchFirmwareVersion", "0x40")]
public partial class BEETouchFirmwareVersion : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _touchFirmwareVersion;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "touchFirmwareVersion")]
	public string TouchFirmwareVersion 
	{
		get { return _touchFirmwareVersion;}
		set { OnSetObject<string>(ref _touchFirmwareVersion, value);} 
	}

	#endregion
}
