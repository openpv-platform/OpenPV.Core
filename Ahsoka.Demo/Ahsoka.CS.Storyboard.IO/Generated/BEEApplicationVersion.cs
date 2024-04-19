using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_ApplicationVersion", "0x40")]
public partial class BEEApplicationVersion : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _applicationVersion;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "applicationVersion")]
	public string ApplicationVersion 
	{
		get { return _applicationVersion;}
		set { OnSetObject<string>(ref _applicationVersion, value);} 
	}

	#endregion
}
