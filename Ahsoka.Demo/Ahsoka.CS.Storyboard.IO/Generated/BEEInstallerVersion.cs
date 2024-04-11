using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_InstallerVersion", "0x40")]
public partial class BEEInstallerVersion : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _installerVersion;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "installerVersion")]
	public string InstallerVersion 
	{
		get { return _installerVersion;}
		set { OnSetObject<string>(ref _installerVersion, value);} 
	}

	#endregion
}
