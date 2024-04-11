using Ahsoka.Storyboard;
using System;

namespace Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_ApplicationName", "0x40")]
public partial class BEEApplicationName : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _applicationName;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "applicationName")]
	public string ApplicationName 
	{
		get { return _applicationName;}
		set { OnSetObject<string>(ref _applicationName, value);} 
	}

	#endregion
}
