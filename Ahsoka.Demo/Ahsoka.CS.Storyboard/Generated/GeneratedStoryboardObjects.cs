using Ahsoka.Storyboard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace  Ahsoka.Demo.Storyboard;

public partial class BrightnessUpdate : DemoViewModelBase
{
	public const string Name = "brightness_update";
	public const string Format = "2u1 percentage";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public UInt16 Percentage
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public BrightnessUpdate() : base(Name, Format, Direction)
	{
	}
	public BrightnessUpdate(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ChangeBrightness : DemoViewModelBase
{
	public const string Name = "change_brightness";
	public const string Format = "2u1 value 1u1 increase";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public UInt16 Value
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Increase
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ChangeBrightness() : base(Name, Format, Direction)
	{
	}
	public ChangeBrightness(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ConnectionStatus : DemoViewModelBase
{
	public const string Name = "connection_status";
	public const string Format = "4u1 signal_level 1s0 ssid";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public UInt32 SignalLevel
	{
		get { return OnGetValue<UInt32>(); }
		set { OnSetValue(value); 
		}
	}
	public String Ssid
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ConnectionStatus() : base(Name, Format, Direction)
	{
	}
	public ConnectionStatus(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class DecreaseTemperature : DemoViewModelBase
{
	public const string Name = "decrease_temperature";
	public const string Format = "2u1 num 2u1 unit";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public UInt16 Num
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	public UInt16 Unit
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public DecreaseTemperature() : base(Name, Format, Direction)
	{
	}
	public DecreaseTemperature(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class IncreaseTemperature : DemoViewModelBase
{
	public const string Name = "increase_temperature";
	public const string Format = "2u1 num 2u1 unit";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public UInt16 Num
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	public UInt16 Unit
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public IncreaseTemperature() : base(Name, Format, Direction)
	{
	}
	public IncreaseTemperature(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class Shutdown : DemoViewModelBase
{
	public const string Name = "shutdown";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public Shutdown() : base(Name, Format, Direction)
	{
	}
	public Shutdown(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ThermostatUpdate : DemoViewModelBase
{
	public const string Name = "thermostat_update";
	public const string Format = "2s1 target_temperature 2s1 current_temperature 1u1 ac 1u1 fan 1u1 timer 1u1 units";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public Int16 TargetTemperature
	{
		get { return OnGetValue<Int16>(); }
		set { OnSetValue(value); 
		}
	}
	public Int16 CurrentTemperature
	{
		get { return OnGetValue<Int16>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Ac
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Fan
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Timer
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte Units
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ThermostatUpdate() : base(Name, Format, Direction)
	{
	}
	public ThermostatUpdate(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ToggleAc : DemoViewModelBase
{
	public const string Name = "toggle_ac";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ToggleAc() : base(Name, Format, Direction)
	{
	}
	public ToggleAc(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ToggleFan : DemoViewModelBase
{
	public const string Name = "toggle_fan";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ToggleFan() : base(Name, Format, Direction)
	{
	}
	public ToggleFan(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ToggleTimer : DemoViewModelBase
{
	public const string Name = "toggle_timer";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ToggleTimer() : base(Name, Format, Direction)
	{
	}
	public ToggleTimer(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ToggleUnits : DemoViewModelBase
{
	public const string Name = "toggle_units";
	public const string Format = "";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ToggleUnits() : base(Name, Format, Direction)
	{
	}
	public ToggleUnits(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class ViewNetwork : DemoViewModelBase
{
	public const string Name = "view_network";
	public const string Format = "4s1 signal_level 1u1 saved_profile 1s0 ssid";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.Out;

	#region Auto Generated Properties
	public Int32 SignalLevel
	{
		get { return OnGetValue<Int32>(); }
		set { OnSetValue(value); 
		}
	}
	public Byte SavedProfile
	{
		get { return OnGetValue<Byte>(); }
		set { OnSetValue(value); 
		}
	}
	public String Ssid
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public ViewNetwork() : base(Name, Format, Direction)
	{
	}
	public ViewNetwork(StoryboardMessage message) : base(message)
	{
	}
	#endregion
}
public partial class WifiMessage : DemoViewModelBase
{
	public const string Name = "wifi_message";
	public const string Format = "2u1 action 1s0 data";
	public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

	#region Auto Generated Properties
	public UInt16 Action
	{
		get { return OnGetValue<UInt16>(); }
		set { OnSetValue(value); 
		}
	}
	public String Data
	{
		get { return OnGetValue<String>(); }
		set { OnSetValue(value); 
		}
	}
	#endregion

	#region Auto Generated Methods
	public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
	public WifiMessage() : base(Name, Format, Direction)
	{
	}
	public WifiMessage(StoryboardMessage message) : base(message)
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
			case BrightnessUpdate.Name:
				return new BrightnessUpdate(data);
			case ChangeBrightness.Name:
				return new ChangeBrightness(data);
			case ConnectionStatus.Name:
				return new ConnectionStatus(data);
			case DecreaseTemperature.Name:
				return new DecreaseTemperature(data);
			case IncreaseTemperature.Name:
				return new IncreaseTemperature(data);
			case Shutdown.Name:
				return new Shutdown(data);
			case ThermostatUpdate.Name:
				return new ThermostatUpdate(data);
			case ToggleAc.Name:
				return new ToggleAc(data);
			case ToggleFan.Name:
				return new ToggleFan(data);
			case ToggleTimer.Name:
				return new ToggleTimer(data);
			case ToggleUnits.Name:
				return new ToggleUnits(data);
			case ViewNetwork.Name:
				return new ViewNetwork(data);
			case WifiMessage.Name:
				return new WifiMessage(data);

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

		// Decode Info for BrightnessUpdate
		metadata.Add(BrightnessUpdate.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(BrightnessUpdate.Percentage),new(1, SBFieldType.Int16_2u, 0, 2, 1, nameof(BrightnessUpdate.Percentage))},
		});

		// Decode Info for ChangeBrightness
		metadata.Add(ChangeBrightness.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(ChangeBrightness.Value),new(1, SBFieldType.Int16_2u, 0, 2, 1, nameof(ChangeBrightness.Value))},
			{nameof(ChangeBrightness.Increase),new(3, SBFieldType.Int8_1u, 2, 1, 1, nameof(ChangeBrightness.Increase))},
		});

		// Decode Info for ConnectionStatus
		metadata.Add(ConnectionStatus.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(ConnectionStatus.SignalLevel),new(1, SBFieldType.Int32_4u, 0, 4, 1, nameof(ConnectionStatus.SignalLevel))},
			{nameof(ConnectionStatus.Ssid),new(3, SBFieldType.String_1s, 4, 1, 0, nameof(ConnectionStatus.Ssid))},
		});

		// Decode Info for DecreaseTemperature
		metadata.Add(DecreaseTemperature.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(DecreaseTemperature.Num),new(1, SBFieldType.Int16_2u, 0, 2, 1, nameof(DecreaseTemperature.Num))},
			{nameof(DecreaseTemperature.Unit),new(3, SBFieldType.Int16_2u, 2, 2, 1, nameof(DecreaseTemperature.Unit))},
		});

		// Decode Info for IncreaseTemperature
		metadata.Add(IncreaseTemperature.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(IncreaseTemperature.Num),new(1, SBFieldType.Int16_2u, 0, 2, 1, nameof(IncreaseTemperature.Num))},
			{nameof(IncreaseTemperature.Unit),new(3, SBFieldType.Int16_2u, 2, 2, 1, nameof(IncreaseTemperature.Unit))},
		});

		// Decode Info for Shutdown
		metadata.Add(Shutdown.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

		// Decode Info for ThermostatUpdate
		metadata.Add(ThermostatUpdate.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(ThermostatUpdate.TargetTemperature),new(1, SBFieldType.SignedInt16_2s, 0, 2, 1, nameof(ThermostatUpdate.TargetTemperature))},
			{nameof(ThermostatUpdate.CurrentTemperature),new(3, SBFieldType.SignedInt16_2s, 2, 2, 1, nameof(ThermostatUpdate.CurrentTemperature))},
			{nameof(ThermostatUpdate.Ac),new(5, SBFieldType.Int8_1u, 4, 1, 1, nameof(ThermostatUpdate.Ac))},
			{nameof(ThermostatUpdate.Fan),new(7, SBFieldType.Int8_1u, 5, 1, 1, nameof(ThermostatUpdate.Fan))},
			{nameof(ThermostatUpdate.Timer),new(9, SBFieldType.Int8_1u, 6, 1, 1, nameof(ThermostatUpdate.Timer))},
			{nameof(ThermostatUpdate.Units),new(11, SBFieldType.Int8_1u, 7, 1, 1, nameof(ThermostatUpdate.Units))},
		});

		// Decode Info for ToggleAc
		metadata.Add(ToggleAc.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

		// Decode Info for ToggleFan
		metadata.Add(ToggleFan.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

		// Decode Info for ToggleTimer
		metadata.Add(ToggleTimer.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

		// Decode Info for ToggleUnits
		metadata.Add(ToggleUnits.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
		});

		// Decode Info for ViewNetwork
		metadata.Add(ViewNetwork.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(ViewNetwork.SignalLevel),new(1, SBFieldType.SignedInt32_4s, 0, 4, 1, nameof(ViewNetwork.SignalLevel))},
			{nameof(ViewNetwork.SavedProfile),new(3, SBFieldType.Int8_1u, 4, 1, 1, nameof(ViewNetwork.SavedProfile))},
			{nameof(ViewNetwork.Ssid),new(5, SBFieldType.String_1s, 5, 1, 0, nameof(ViewNetwork.Ssid))},
		});

		// Decode Info for WifiMessage
		metadata.Add(WifiMessage.Name, new Dictionary<string, StoryboardPropertyInfo>()
		{
			{nameof(WifiMessage.Action),new(1, SBFieldType.Int16_2u, 0, 2, 1, nameof(WifiMessage.Action))},
			{nameof(WifiMessage.Data),new(3, SBFieldType.String_1s, 2, 1, 0, nameof(WifiMessage.Data))},
		});

    }
}

