using Ahsoka.Storyboard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace  Ahsoka.Application.Storyboard;

public partial class BEEAnalogInput1Status : ApplicationViewModelBase
{
	public const string Name = "BEE_AnalogInput1Status";
	public const string Format = "4f1 value";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public float Value
	{
		get { return OnGetValue<float>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEAnalogInput1Status() : base(Name, Format, Direction)
	{
	}
	public BEEAnalogInput1Status(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEAnalogInput2Status : ApplicationViewModelBase
{
	public const string Name = "BEE_AnalogInput2Status";
	public const string Format = "4f1 value";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public float Value
	{
		get { return OnGetValue<float>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEAnalogInput2Status() : base(Name, Format, Direction)
	{
	}
	public BEEAnalogInput2Status(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEApplicationName : ApplicationViewModelBase
{
	public const string Name = "BEE_ApplicationName";
	public const string Format = "1s0 applicationName";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String ApplicationName
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEApplicationName() : base(Name, Format, Direction)
	{
	}
	public BEEApplicationName(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEApplicationVersion : ApplicationViewModelBase
{
	public const string Name = "BEE_ApplicationVersion";
	public const string Format = "1s0 applicationVersion";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String ApplicationVersion
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEApplicationVersion() : base(Name, Format, Direction)
	{
	}
	public BEEApplicationVersion(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEBrightnessStatus : ApplicationViewModelBase
{
	public const string Name = "BEE_BrightnessStatus";
	public const string Format = "1u1 percent";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public Byte Percent
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEBrightnessStatus() : base(Name, Format, Direction)
	{
	}
	public BEEBrightnessStatus(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEBSPVersion : ApplicationViewModelBase
{
	public const string Name = "BEE_BSPVersion";
	public const string Format = "1s0 bspVersion";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String BspVersion
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEBSPVersion() : base(Name, Format, Direction)
	{
	}
	public BEEBSPVersion(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEBuzzerStatus : ApplicationViewModelBase
{
	public const string Name = "BEE_BuzzerStatus";
	public const string Format = "4u1 frequency 1u1 volumePercent 1u1 enable";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public UInt32 Frequency
	{
		get { return OnGetValue<UInt32>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte VolumePercent
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Enable
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEBuzzerStatus() : base(Name, Format, Direction)
	{
	}
	public BEEBuzzerStatus(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEDeviceId : ApplicationViewModelBase
{
	public const string Name = "BEE_DeviceId";
	public const string Format = "1s0 deviceId";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String DeviceId
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEDeviceId() : base(Name, Format, Direction)
	{
	}
	public BEEDeviceId(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEDigitalInput1Status : ApplicationViewModelBase
{
	public const string Name = "BEE_DigitalInput1Status";
	public const string Format = "4f1 value";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public float Value
	{
		get { return OnGetValue<float>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEDigitalInput1Status() : base(Name, Format, Direction)
	{
	}
	public BEEDigitalInput1Status(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEDigitalInput2Status : ApplicationViewModelBase
{
	public const string Name = "BEE_DigitalInput2Status";
	public const string Format = "4f1 value";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public float Value
	{
		get { return OnGetValue<float>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEDigitalInput2Status() : base(Name, Format, Direction)
	{
	}
	public BEEDigitalInput2Status(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEDigitalOutput1Status : ApplicationViewModelBase
{
	public const string Name = "BEE_DigitalOutput1Status";
	public const string Format = "4s1 state";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public Int32 State
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEDigitalOutput1Status() : base(Name, Format, Direction)
	{
	}
	public BEEDigitalOutput1Status(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEDigitalOutput2Status : ApplicationViewModelBase
{
	public const string Name = "BEE_DigitalOutput2Status";
	public const string Format = "4s1 state";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public Int32 State
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEDigitalOutput2Status() : base(Name, Format, Direction)
	{
	}
	public BEEDigitalOutput2Status(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEInstallerVersion : ApplicationViewModelBase
{
	public const string Name = "BEE_InstallerVersion";
	public const string Format = "1s0 installerVersion";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String InstallerVersion
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEInstallerVersion() : base(Name, Format, Direction)
	{
	}
	public BEEInstallerVersion(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEEPartNumber : ApplicationViewModelBase
{
	public const string Name = "BEE_PartNumber";
	public const string Format = "1s0 partNumber";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String PartNumber
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEEPartNumber() : base(Name, Format, Direction)
	{
	}
	public BEEPartNumber(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEESDKVersion : ApplicationViewModelBase
{
	public const string Name = "BEE_SDKVersion";
	public const string Format = "1s0 sdkVersion";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String SdkVersion
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEESDKVersion() : base(Name, Format, Direction)
	{
	}
	public BEESDKVersion(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEESerialNumber : ApplicationViewModelBase
{
	public const string Name = "BEE_SerialNumber";
	public const string Format = "1s0 serialNumber";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String SerialNumber
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEESerialNumber() : base(Name, Format, Direction)
	{
	}
	public BEESerialNumber(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEETouchFirmwareVersion : ApplicationViewModelBase
{
	public const string Name = "BEE_TouchFirmwareVersion";
	public const string Format = "1s0 touchFirmwareVersion";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String TouchFirmwareVersion
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEETouchFirmwareVersion() : base(Name, Format, Direction)
	{
	}
	public BEETouchFirmwareVersion(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class BEETouchHardwareVersion : ApplicationViewModelBase
{
	public const string Name = "BEE_TouchHardwareVersion";
	public const string Format = "1s0 touchHardwareVersion";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public String TouchHardwareVersion
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BEETouchHardwareVersion() : base(Name, Format, Direction)
	{
	}
	public BEETouchHardwareVersion(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class DEVBackend : ApplicationViewModelBase
{
	public const string Name = "DEV_Backend";
	public const string Format = "8u1 var_uint64 8s1 var_int64 4f1 var_float 4u1 var_uint32 4s1 var_int32 2u1 var_uint16 2s1 var_int16 1u1 var_uint8 1s1 var_int8 1s0 var_string";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public UInt64 VarUint64
	{
		get { return OnGetValue<UInt64>(); }
		set { OnSetValue(value); 
		}
	}
	public Int64 VarInt64
	{
		get { return OnGetValue<Int64>(); }
		set { OnSetValue(value); 
		}
	}
	public float VarFloat
	{
		get { return OnGetValue<float>(); }
		set { OnSetValue(value); 
		}
	}
	public UInt32 VarUint32
	{
		get { return OnGetValue<UInt32>(); }
		set { OnSetValue(value); 
		}
	}
	public Int32 VarInt32
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	public UInt16 VarUint16
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	public Int16 VarInt16
	{
		get { return OnGetValue<Int16>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte VarUint8
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public SByte VarInt8
	{
		get { return OnGetValue<SByte>(); }
		set { OnSetValue(value); 
		}
	}
	public String VarString
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public DEVBackend() : base(Name, Format, Direction)
	{
	}
	public DEVBackend(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class DEVFrontend : ApplicationViewModelBase
{
	public const string Name = "DEV_Frontend";
	public const string Format = "8u1 var_uint64 8s1 var_int64 4f1 var_float 4u1 var_uint32 4s1 var_int32 2u1 var_uint16 2s1 var_int16 1u1 var_uint8 1s1 var_int8 1s0 var_string";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public UInt64 VarUint64
	{
		get { return OnGetValue<UInt64>(); }
		set { OnSetValue(value); 
		}
	}
	public Int64 VarInt64
	{
		get { return OnGetValue<Int64>(); }
		set { OnSetValue(value); 
		}
	}
	public float VarFloat
	{
		get { return OnGetValue<float>(); }
		set { OnSetValue(value); 
		}
	}
	public UInt32 VarUint32
	{
		get { return OnGetValue<UInt32>(); }
		set { OnSetValue(value); 
		}
	}
	public Int32 VarInt32
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	public UInt16 VarUint16
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	public Int16 VarInt16
	{
		get { return OnGetValue<Int16>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte VarUint8
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public SByte VarInt8
	{
		get { return OnGetValue<SByte>(); }
		set { OnSetValue(value); 
		}
	}
	public String VarString
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public DEVFrontend() : base(Name, Format, Direction)
	{
	}
	public DEVFrontend(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class FEEBrightnessRequest : ApplicationViewModelBase
{
	public const string Name = "FEE_BrightnessRequest";
	public const string Format = "1u1 percent";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public Byte Percent
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public FEEBrightnessRequest() : base(Name, Format, Direction)
	{
	}
	public FEEBrightnessRequest(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class FEEBuzzerRequest : ApplicationViewModelBase
{
	public const string Name = "FEE_BuzzerRequest";
	public const string Format = "4u1 frequency 1u1 volumePercent 1u1 enable";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public UInt32 Frequency
	{
		get { return OnGetValue<UInt32>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte VolumePercent
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Enable
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public FEEBuzzerRequest() : base(Name, Format, Direction)
	{
	}
	public FEEBuzzerRequest(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class FEEDigitalOutput1Request : ApplicationViewModelBase
{
	public const string Name = "FEE_DigitalOutput1Request";
	public const string Format = "4s1 state";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public Int32 State
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public FEEDigitalOutput1Request() : base(Name, Format, Direction)
	{
	}
	public FEEDigitalOutput1Request(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class FEEDigitalOutput2Request : ApplicationViewModelBase
{
	public const string Name = "FEE_DigitalOutput2Request";
	public const string Format = "4s1 state";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public Int32 State
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public FEEDigitalOutput2Request() : base(Name, Format, Direction)
	{
	}
	public FEEDigitalOutput2Request(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class FEEShutdownRequest : ApplicationViewModelBase
{
	public const string Name = "FEE_ShutdownRequest";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public FEEShutdownRequest() : base(Name, Format, Direction)
	{
	}
	public FEEShutdownRequest(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class FEESystemInfoRequest : ApplicationViewModelBase
{
	public const string Name = "FEE_SystemInfoRequest";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public FEESystemInfoRequest() : base(Name, Format, Direction)
	{
	}
	public FEESystemInfoRequest(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}

public class StoryboardMetadata : IStoryboardMetadata
{
    Dictionary<string, Dictionary<string, StoryboardPropertyInfo>> metadata = new();

    static StoryboardMetadata eventMetadata = null;

    public Dictionary<string, Dictionary<string, StoryboardPropertyInfo>> GetMetadata() { return metadata; }

    public Dictionary<string, StoryboardPropertyInfo> GetMetadata(string eventName) { return metadata[eventName]; }

    public void AddMetadata(string eventName, Dictionary<string, StoryboardPropertyInfo> properties)
    {
        metadata.Add(eventName, properties);
    }

    public StoryboardPropertyInfo GetPropertyInfo(string eventName, string fieldName)
    {
        return metadata[eventName].Values.FirstOrDefault(x => x.FieldName == fieldName);
    }

    public StoryboardViewModelBase CreateObject(StoryboardMessage data)
    {
        switch (data.Name)
        {
			case BEEAnalogInput1Status.Name:
				return new BEEAnalogInput1Status(data);
			case BEEAnalogInput2Status.Name:
				return new BEEAnalogInput2Status(data);
			case BEEApplicationName.Name:
				return new BEEApplicationName(data);
			case BEEApplicationVersion.Name:
				return new BEEApplicationVersion(data);
			case BEEBrightnessStatus.Name:
				return new BEEBrightnessStatus(data);
			case BEEBSPVersion.Name:
				return new BEEBSPVersion(data);
			case BEEBuzzerStatus.Name:
				return new BEEBuzzerStatus(data);
			case BEEDeviceId.Name:
				return new BEEDeviceId(data);
			case BEEDigitalInput1Status.Name:
				return new BEEDigitalInput1Status(data);
			case BEEDigitalInput2Status.Name:
				return new BEEDigitalInput2Status(data);
			case BEEDigitalOutput1Status.Name:
				return new BEEDigitalOutput1Status(data);
			case BEEDigitalOutput2Status.Name:
				return new BEEDigitalOutput2Status(data);
			case BEEInstallerVersion.Name:
				return new BEEInstallerVersion(data);
			case BEEPartNumber.Name:
				return new BEEPartNumber(data);
			case BEESDKVersion.Name:
				return new BEESDKVersion(data);
			case BEESerialNumber.Name:
				return new BEESerialNumber(data);
			case BEETouchFirmwareVersion.Name:
				return new BEETouchFirmwareVersion(data);
			case BEETouchHardwareVersion.Name:
				return new BEETouchHardwareVersion(data);
			case DEVBackend.Name:
				return new DEVBackend(data);
			case DEVFrontend.Name:
				return new DEVFrontend(data);
			case FEEBrightnessRequest.Name:
				return new FEEBrightnessRequest(data);
			case FEEBuzzerRequest.Name:
				return new FEEBuzzerRequest(data);
			case FEEDigitalOutput1Request.Name:
				return new FEEDigitalOutput1Request(data);
			case FEEDigitalOutput2Request.Name:
				return new FEEDigitalOutput2Request(data);
			case FEEShutdownRequest.Name:
				return new FEEShutdownRequest(data);
			case FEESystemInfoRequest.Name:
				return new FEESystemInfoRequest(data);

            default:
                return null;
        }
    }

    public static StoryboardMetadata EventMetadata
    {
        get
        {
            if (eventMetadata == null)
                eventMetadata = new();
            return eventMetadata;
        }
    }

    StoryboardMetadata()
    {

		// Decode Info for BEEAnalogInput1Status
		metadata.Add(BEEAnalogInput1Status.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEAnalogInput1Status.Value),new(1, SBFieldType.Float_4f, 0, 4, 1, nameof(BEEAnalogInput1Status.Value))},
		});

		// Decode Info for BEEAnalogInput2Status
		metadata.Add(BEEAnalogInput2Status.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEAnalogInput2Status.Value),new(1, SBFieldType.Float_4f, 0, 4, 1, nameof(BEEAnalogInput2Status.Value))},
		});

		// Decode Info for BEEApplicationName
		metadata.Add(BEEApplicationName.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEApplicationName.ApplicationName),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEEApplicationName.ApplicationName))},
		});

		// Decode Info for BEEApplicationVersion
		metadata.Add(BEEApplicationVersion.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEApplicationVersion.ApplicationVersion),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEEApplicationVersion.ApplicationVersion))},
		});

		// Decode Info for BEEBrightnessStatus
		metadata.Add(BEEBrightnessStatus.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEBrightnessStatus.Percent),new(1, SBFieldType.Int8_1u, 0, 1, 1, nameof(BEEBrightnessStatus.Percent))},
		});

		// Decode Info for BEEBSPVersion
		metadata.Add(BEEBSPVersion.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEBSPVersion.BspVersion),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEEBSPVersion.BspVersion))},
		});

		// Decode Info for BEEBuzzerStatus
		metadata.Add(BEEBuzzerStatus.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEBuzzerStatus.Frequency),new(1, SBFieldType.Int32_4u, 0, 4, 1, nameof(BEEBuzzerStatus.Frequency))},
			{nameof(BEEBuzzerStatus.VolumePercent),new(3, SBFieldType.Int8_1u, 4, 1, 1, nameof(BEEBuzzerStatus.VolumePercent))},
			{nameof(BEEBuzzerStatus.Enable),new(5, SBFieldType.Int8_1u, 5, 1, 1, nameof(BEEBuzzerStatus.Enable))},
		});

		// Decode Info for BEEDeviceId
		metadata.Add(BEEDeviceId.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEDeviceId.DeviceId),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEEDeviceId.DeviceId))},
		});

		// Decode Info for BEEDigitalInput1Status
		metadata.Add(BEEDigitalInput1Status.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEDigitalInput1Status.Value),new(1, SBFieldType.Float_4f, 0, 4, 1, nameof(BEEDigitalInput1Status.Value))},
		});

		// Decode Info for BEEDigitalInput2Status
		metadata.Add(BEEDigitalInput2Status.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEDigitalInput2Status.Value),new(1, SBFieldType.Float_4f, 0, 4, 1, nameof(BEEDigitalInput2Status.Value))},
		});

		// Decode Info for BEEDigitalOutput1Status
		metadata.Add(BEEDigitalOutput1Status.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEDigitalOutput1Status.State),new(1, SBFieldType.SignedInt32_4s, 0, 4, 1, nameof(BEEDigitalOutput1Status.State))},
		});

		// Decode Info for BEEDigitalOutput2Status
		metadata.Add(BEEDigitalOutput2Status.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEDigitalOutput2Status.State),new(1, SBFieldType.SignedInt32_4s, 0, 4, 1, nameof(BEEDigitalOutput2Status.State))},
		});

		// Decode Info for BEEInstallerVersion
		metadata.Add(BEEInstallerVersion.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEInstallerVersion.InstallerVersion),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEEInstallerVersion.InstallerVersion))},
		});

		// Decode Info for BEEPartNumber
		metadata.Add(BEEPartNumber.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEEPartNumber.PartNumber),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEEPartNumber.PartNumber))},
		});

		// Decode Info for BEESDKVersion
		metadata.Add(BEESDKVersion.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEESDKVersion.SdkVersion),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEESDKVersion.SdkVersion))},
		});

		// Decode Info for BEESerialNumber
		metadata.Add(BEESerialNumber.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEESerialNumber.SerialNumber),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEESerialNumber.SerialNumber))},
		});

		// Decode Info for BEETouchFirmwareVersion
		metadata.Add(BEETouchFirmwareVersion.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEETouchFirmwareVersion.TouchFirmwareVersion),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEETouchFirmwareVersion.TouchFirmwareVersion))},
		});

		// Decode Info for BEETouchHardwareVersion
		metadata.Add(BEETouchHardwareVersion.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BEETouchHardwareVersion.TouchHardwareVersion),new(1, SBFieldType.String_1s, 0, 1, 0, nameof(BEETouchHardwareVersion.TouchHardwareVersion))},
		});

		// Decode Info for DEVBackend
		metadata.Add(DEVBackend.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(DEVBackend.VarUint64),new(1, SBFieldType.Int64_8u, 0, 8, 1, nameof(DEVBackend.VarUint64))},
			{nameof(DEVBackend.VarInt64),new(3, SBFieldType.SignedInt64_8s, 8, 8, 1, nameof(DEVBackend.VarInt64))},
			{nameof(DEVBackend.VarFloat),new(5, SBFieldType.Float_4f, 16, 4, 1, nameof(DEVBackend.VarFloat))},
			{nameof(DEVBackend.VarUint32),new(7, SBFieldType.Int32_4u, 20, 4, 1, nameof(DEVBackend.VarUint32))},
			{nameof(DEVBackend.VarInt32),new(9, SBFieldType.SignedInt32_4s, 24, 4, 1, nameof(DEVBackend.VarInt32))},
			{nameof(DEVBackend.VarUint16),new(11, SBFieldType.Int16_2u, 28, 2, 1, nameof(DEVBackend.VarUint16))},
			{nameof(DEVBackend.VarInt16),new(13, SBFieldType.SignedInt16_2s, 30, 2, 1, nameof(DEVBackend.VarInt16))},
			{nameof(DEVBackend.VarUint8),new(15, SBFieldType.Int8_1u, 32, 1, 1, nameof(DEVBackend.VarUint8))},
			{nameof(DEVBackend.VarInt8),new(17, SBFieldType.SignedInt8_1s, 33, 1, 1, nameof(DEVBackend.VarInt8))},
			{nameof(DEVBackend.VarString),new(19, SBFieldType.String_1s, 34, 1, 0, nameof(DEVBackend.VarString))},
		});

		// Decode Info for DEVFrontend
		metadata.Add(DEVFrontend.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(DEVFrontend.VarUint64),new(1, SBFieldType.Int64_8u, 0, 8, 1, nameof(DEVFrontend.VarUint64))},
			{nameof(DEVFrontend.VarInt64),new(3, SBFieldType.SignedInt64_8s, 8, 8, 1, nameof(DEVFrontend.VarInt64))},
			{nameof(DEVFrontend.VarFloat),new(5, SBFieldType.Float_4f, 16, 4, 1, nameof(DEVFrontend.VarFloat))},
			{nameof(DEVFrontend.VarUint32),new(7, SBFieldType.Int32_4u, 20, 4, 1, nameof(DEVFrontend.VarUint32))},
			{nameof(DEVFrontend.VarInt32),new(9, SBFieldType.SignedInt32_4s, 24, 4, 1, nameof(DEVFrontend.VarInt32))},
			{nameof(DEVFrontend.VarUint16),new(11, SBFieldType.Int16_2u, 28, 2, 1, nameof(DEVFrontend.VarUint16))},
			{nameof(DEVFrontend.VarInt16),new(13, SBFieldType.SignedInt16_2s, 30, 2, 1, nameof(DEVFrontend.VarInt16))},
			{nameof(DEVFrontend.VarUint8),new(15, SBFieldType.Int8_1u, 32, 1, 1, nameof(DEVFrontend.VarUint8))},
			{nameof(DEVFrontend.VarInt8),new(17, SBFieldType.SignedInt8_1s, 33, 1, 1, nameof(DEVFrontend.VarInt8))},
			{nameof(DEVFrontend.VarString),new(19, SBFieldType.String_1s, 34, 1, 0, nameof(DEVFrontend.VarString))},
		});

		// Decode Info for FEEBrightnessRequest
		metadata.Add(FEEBrightnessRequest.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(FEEBrightnessRequest.Percent),new(1, SBFieldType.Int8_1u, 0, 1, 1, nameof(FEEBrightnessRequest.Percent))},
		});

		// Decode Info for FEEBuzzerRequest
		metadata.Add(FEEBuzzerRequest.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(FEEBuzzerRequest.Frequency),new(1, SBFieldType.Int32_4u, 0, 4, 1, nameof(FEEBuzzerRequest.Frequency))},
			{nameof(FEEBuzzerRequest.VolumePercent),new(3, SBFieldType.Int8_1u, 4, 1, 1, nameof(FEEBuzzerRequest.VolumePercent))},
			{nameof(FEEBuzzerRequest.Enable),new(5, SBFieldType.Int8_1u, 5, 1, 1, nameof(FEEBuzzerRequest.Enable))},
		});

		// Decode Info for FEEDigitalOutput1Request
		metadata.Add(FEEDigitalOutput1Request.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(FEEDigitalOutput1Request.State),new(1, SBFieldType.SignedInt32_4s, 0, 4, 1, nameof(FEEDigitalOutput1Request.State))},
		});

		// Decode Info for FEEDigitalOutput2Request
		metadata.Add(FEEDigitalOutput2Request.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(FEEDigitalOutput2Request.State),new(1, SBFieldType.SignedInt32_4s, 0, 4, 1, nameof(FEEDigitalOutput2Request.State))},
		});

		// Decode Info for FEEShutdownRequest
		metadata.Add(FEEShutdownRequest.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

		// Decode Info for FEESystemInfoRequest
		metadata.Add(FEESystemInfoRequest.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

    }
}

