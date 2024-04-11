using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_TouchHardwareVersion", "0x40")]
public partial class BEETouchHardwareVersion : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _touchHardwareVersion;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "touchHardwareVersion")]
	public string TouchHardwareVersion 
	{
		get { return _touchHardwareVersion;}
		set { OnSetObject<string>(ref _touchHardwareVersion, value);} 
	}

	#endregion
}
