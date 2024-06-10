#pragma warning disable CS1591
using System;
using System.Text;

namespace Ahsoka.Services.Bluetooth;

public partial class BluetoothAdapterSetup
{
    #region Class Of Device properties
    public bool LimitedDiscoverableModeService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.LimitedDiscoverableMode); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.LimitedDiscoverableMode;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.LimitedDiscoverableMode);
        }
    }

    public bool PositioningService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Positioning); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Positioning;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Positioning);
        }
    }

    public bool NetworkingService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Networking); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Networking;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Networking);
        }
    }

    public bool RenderingService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Rendering); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Rendering;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Rendering);
        }
    }

    public bool CapturingService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Capturing); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Capturing;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Capturing);
        }
    }

    public bool ObjectTransferService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.ObjectTransfer); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.ObjectTransfer;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.ObjectTransfer);
        }
    }

    public bool AudioService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Audio); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Audio;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Audio);
        }
    }

    public bool TelephonyService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Telephony); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Telephony;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Telephony);
        }
    }

    public bool InformationService
    {
        get { return this.ServiceClass.HasFlag(BluetoothMajorServiceClass.Information); }
        set
        {
            if (value)
                this.ServiceClass |= BluetoothMajorServiceClass.Information;
            else
                this.ServiceClass &= (~BluetoothMajorServiceClass.Information);
        }
    }

    public string ServiceClassDescription
    {
        get
        {
            if (this.ServiceClass != 0)
            {
                StringBuilder builder = new();
                foreach (BluetoothMajorServiceClass item in Enum.GetValues(typeof(BluetoothMajorServiceClass)))
                {
                    if (this.ServiceClass.HasFlag(item))
                    {
                        builder.Append($"{item.ToString()}, ");
                    }
                }

                // Remove trailing comma
                if (builder.Length >= 2)
                    builder.Remove(builder.Length - 2, 2);

                return builder.ToString();
            }
            else
                return "(None)";
        }
    }

    #endregion

    #region Profile Properties

    public bool SppEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Spp); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Spp;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Spp;
        }
    }

    public bool HfpAgEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.HfpAg); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.HfpAg;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.HfpAg;
        }
    }

    public bool HfpEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Hfp); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Hfp;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Hfp;
        }
    }

    public bool OppEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Opp); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Opp;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Opp;
        }
    }

    public bool AudioSinkEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.A2dpSink); }
        set
        {
            if (value)
            {
                this.SupportedProfiles |= BluetoothProfiles.A2dpSink;
                this.SupportedProfiles |= BluetoothProfiles.AvrcpSource;
            }
            else
            {
                this.SupportedProfiles &= ~BluetoothProfiles.A2dpSink;
                this.SupportedProfiles &= ~BluetoothProfiles.AvrcpSource;
            }
        }
    }

    public bool HeadsetGateway
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.HspAg); }
        set
        {
            if (value)
            {
                this.SupportedProfiles |= BluetoothProfiles.HspAg;
                //this.SupportedProfiles |= BluetoothProfiles.HfpAg;
            }
            else
            {
                this.SupportedProfiles &= ~BluetoothProfiles.HspAg;
                //this.SupportedProfiles &= ~BluetoothProfiles.HfpAg;
            }
        }
    }


    public bool AudioSourceEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.A2dpSource); }
        set
        {
            if (value)
            {
                this.SupportedProfiles |= BluetoothProfiles.A2dpSource;
                this.SupportedProfiles |= BluetoothProfiles.AvrcpTarget;
            }
            else
            {
                this.SupportedProfiles &= ~BluetoothProfiles.A2dpSource;
                this.SupportedProfiles &= ~BluetoothProfiles.AvrcpTarget;
            }
        }
    }

    public bool HidEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Hid); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Hid;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Hid;
        }
    }

    public bool HspAgEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.HspAg); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.HspAg;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.HspAg;
        }
    }

    public bool HspEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Hsp); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Hsp;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Hsp;
        }
    }

    public bool HdpSinkEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.HdpSink); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.HdpSink;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.HdpSink;
        }
    }

    public bool HdpSourceEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.HdpSource); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.HdpSource;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.HdpSource;
        }
    }

    public bool PbapEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Pbap); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Pbap;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Pbap;
        }
    }

    public bool BgioEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Bgio); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Bgio;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Bgio;
        }
    }

    public bool OtaEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Ota); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Ota;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Ota;
        }
    }

    public bool MapEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Map); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Map;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Map;
        }
    }

    public bool IapEnabled
    {
        get { return this.SupportedProfiles.HasFlag(BluetoothProfiles.Iap); }
        set
        {
            if (value)
                this.SupportedProfiles |= BluetoothProfiles.Iap;
            else
                this.SupportedProfiles &= ~BluetoothProfiles.Iap;
        }
    }
    #endregion

    #region Methods
    public BluetoothAdapterSetup()
    {
        ServiceClass = BluetoothMajorServiceClass.Audio;
        MajorDeviceClass = BluetoothMajorDeviceClass.AudioVideo;
        MinorDeviceClass = (int)AudioVideoMinorDeviceClass.CarAudio;
    }

    #endregion
}