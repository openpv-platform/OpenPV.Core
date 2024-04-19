using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_SDKVersion", "0x40")]
public partial class BEESDKVersion : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _sdkVersion;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "sdkVersion")]
	public string SdkVersion 
	{
		get { return _sdkVersion;}
		set { OnSetObject<string>(ref _sdkVersion, value);} 
	}

	#endregion
}
