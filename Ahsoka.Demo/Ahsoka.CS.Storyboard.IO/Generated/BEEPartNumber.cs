using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("BEE_PartNumber", "0x40")]
public partial class BEEPartNumber : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private string _partNumber;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 0, "partNumber")]
	public string PartNumber 
	{
		get { return _partNumber;}
		set { OnSetObject<string>(ref _partNumber, value);} 
	}

	#endregion
}
