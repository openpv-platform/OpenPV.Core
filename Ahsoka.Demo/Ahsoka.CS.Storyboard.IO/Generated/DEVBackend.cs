using Ahsoka.Storyboard;
using System;

namespace  Ahsoka.Application.Storyboard;

[SBMessageEventName("DEV_Backend", "0x40")]
public partial class DEVBackend : ApplicationViewModelBase
{
	#region Auto Generated Fields
	private UInt64 _var_uint64;
	private Int64 _var_int64;
	private float _var_float;
	private UInt32 _var_uint32;
	private Int32 _var_int32;
	private UInt16 _var_uint16;
	private Int16 _var_int16;
	private Byte _var_uint8;
	private SByte _var_int8;
	private string _var_string;
	#endregion

	#region Auto Generated Properties

	[SBField(1, 1, "var_uint64")]
	public UInt64 VarUint64 
	{
		get { return _var_uint64;}
		set { OnSetValue<UInt64>(ref _var_uint64, value);} 
	}

	[SBField(3, 1, "var_int64")]
	public Int64 VarInt64 
	{
		get { return _var_int64;}
		set { OnSetValue<Int64>(ref _var_int64, value);} 
	}

	[SBField(5, 1, "var_float")]
	public float VarFloat 
	{
		get { return _var_float;}
		set { OnSetValue<float>(ref _var_float, value);} 
	}

	[SBField(7, 1, "var_uint32")]
	public UInt32 VarUint32 
	{
		get { return _var_uint32;}
		set { OnSetValue<UInt32>(ref _var_uint32, value);} 
	}

	[SBField(9, 1, "var_int32")]
	public Int32 VarInt32 
	{
		get { return _var_int32;}
		set { OnSetValue<Int32>(ref _var_int32, value);} 
	}

	[SBField(11, 1, "var_uint16")]
	public UInt16 VarUint16 
	{
		get { return _var_uint16;}
		set { OnSetValue<UInt16>(ref _var_uint16, value);} 
	}

	[SBField(13, 1, "var_int16")]
	public Int16 VarInt16 
	{
		get { return _var_int16;}
		set { OnSetValue<Int16>(ref _var_int16, value);} 
	}

	[SBField(15, 1, "var_uint8")]
	public Byte VarUint8 
	{
		get { return _var_uint8;}
		set { OnSetValue<Byte>(ref _var_uint8, value);} 
	}

	[SBField(17, 1, "var_int8")]
	public SByte VarInt8 
	{
		get { return _var_int8;}
		set { OnSetValue<SByte>(ref _var_int8, value);} 
	}

	[SBField(19, 0, "var_string")]
	public string VarString 
	{
		get { return _var_string;}
		set { OnSetObject<string>(ref _var_string, value);} 
	}

	#endregion
}
