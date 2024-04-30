using Ahsoka.Storyboard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace  Ahsoka.Application.Storyboard;

public partial class DEVBackendToFrontend : ApplicationViewModelBase
{
	public const string Name = "DEV_BackendToFrontend";
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
	public DEVBackendToFrontend() : base(Name, Format, Direction)
	{
	}
	public DEVBackendToFrontend(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class DEVFrontendToBackend : ApplicationViewModelBase
{
	public const string Name = "DEV_FrontendToBackend";
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
	public DEVFrontendToBackend() : base(Name, Format, Direction)
	{
	}
	public DEVFrontendToBackend(StoryboardMessage message) : base(message)
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
			case DEVBackendToFrontend.Name:
				return new DEVBackendToFrontend(data);
			case DEVFrontendToBackend.Name:
				return new DEVFrontendToBackend(data);

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

		// Decode Info for DEVBackendToFrontend
		metadata.Add(DEVBackendToFrontend.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(DEVBackendToFrontend.VarUint64),new(1, SBFieldType.Int64_8u, 0, 8, 1, nameof(DEVBackendToFrontend.VarUint64))},
			{nameof(DEVBackendToFrontend.VarInt64),new(3, SBFieldType.SignedInt64_8s, 8, 8, 1, nameof(DEVBackendToFrontend.VarInt64))},
			{nameof(DEVBackendToFrontend.VarFloat),new(5, SBFieldType.Float_4f, 16, 4, 1, nameof(DEVBackendToFrontend.VarFloat))},
			{nameof(DEVBackendToFrontend.VarUint32),new(7, SBFieldType.Int32_4u, 20, 4, 1, nameof(DEVBackendToFrontend.VarUint32))},
			{nameof(DEVBackendToFrontend.VarInt32),new(9, SBFieldType.SignedInt32_4s, 24, 4, 1, nameof(DEVBackendToFrontend.VarInt32))},
			{nameof(DEVBackendToFrontend.VarUint16),new(11, SBFieldType.Int16_2u, 28, 2, 1, nameof(DEVBackendToFrontend.VarUint16))},
			{nameof(DEVBackendToFrontend.VarInt16),new(13, SBFieldType.SignedInt16_2s, 30, 2, 1, nameof(DEVBackendToFrontend.VarInt16))},
			{nameof(DEVBackendToFrontend.VarUint8),new(15, SBFieldType.Int8_1u, 32, 1, 1, nameof(DEVBackendToFrontend.VarUint8))},
			{nameof(DEVBackendToFrontend.VarInt8),new(17, SBFieldType.SignedInt8_1s, 33, 1, 1, nameof(DEVBackendToFrontend.VarInt8))},
			{nameof(DEVBackendToFrontend.VarString),new(19, SBFieldType.String_1s, 34, 1, 0, nameof(DEVBackendToFrontend.VarString))},
		});

		// Decode Info for DEVFrontendToBackend
		metadata.Add(DEVFrontendToBackend.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(DEVFrontendToBackend.VarUint64),new(1, SBFieldType.Int64_8u, 0, 8, 1, nameof(DEVFrontendToBackend.VarUint64))},
			{nameof(DEVFrontendToBackend.VarInt64),new(3, SBFieldType.SignedInt64_8s, 8, 8, 1, nameof(DEVFrontendToBackend.VarInt64))},
			{nameof(DEVFrontendToBackend.VarFloat),new(5, SBFieldType.Float_4f, 16, 4, 1, nameof(DEVFrontendToBackend.VarFloat))},
			{nameof(DEVFrontendToBackend.VarUint32),new(7, SBFieldType.Int32_4u, 20, 4, 1, nameof(DEVFrontendToBackend.VarUint32))},
			{nameof(DEVFrontendToBackend.VarInt32),new(9, SBFieldType.SignedInt32_4s, 24, 4, 1, nameof(DEVFrontendToBackend.VarInt32))},
			{nameof(DEVFrontendToBackend.VarUint16),new(11, SBFieldType.Int16_2u, 28, 2, 1, nameof(DEVFrontendToBackend.VarUint16))},
			{nameof(DEVFrontendToBackend.VarInt16),new(13, SBFieldType.SignedInt16_2s, 30, 2, 1, nameof(DEVFrontendToBackend.VarInt16))},
			{nameof(DEVFrontendToBackend.VarUint8),new(15, SBFieldType.Int8_1u, 32, 1, 1, nameof(DEVFrontendToBackend.VarUint8))},
			{nameof(DEVFrontendToBackend.VarInt8),new(17, SBFieldType.SignedInt8_1s, 33, 1, 1, nameof(DEVFrontendToBackend.VarInt8))},
			{nameof(DEVFrontendToBackend.VarString),new(19, SBFieldType.String_1s, 34, 1, 0, nameof(DEVFrontendToBackend.VarString))},
		});

    }
}

