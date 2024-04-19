using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_SerialNumber", "0x40")]
public partial class BEESerialNumber : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _serialNumber;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "serialNumber")]
	public string SerialNumber 
	{
		get { return _serialNumber;}
		set { OnSetObject<string>(ref _serialNumber, value);} 
	}

	#endregion
}
