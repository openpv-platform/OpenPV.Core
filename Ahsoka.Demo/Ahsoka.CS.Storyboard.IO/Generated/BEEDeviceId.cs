using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_DeviceId", "0x40")]
public partial class BEEDeviceId : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _deviceId;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "deviceId")]
	public string DeviceId 
	{
		get { return _deviceId;}
		set { OnSetObject<string>(ref _deviceId, value);} 
	}

	#endregion
}
