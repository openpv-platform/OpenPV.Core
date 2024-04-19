using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_BSPVersion", "0x40")]
public partial class BEEBSPVersion : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _bspVersion;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "bspVersion")]
	public string BspVersion 
	{
		get { return _bspVersion;}
		set { OnSetObject<string>(ref _bspVersion, value);} 
	}

	#endregion
}
