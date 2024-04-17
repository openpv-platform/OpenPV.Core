using System;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.Utility;

namespace Ahsoka.Application.Storyboard;

public partial class BEEBuzzerStatus
{

    public override void OnUpdateStoryboard()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"C# Buzzer Status");

        BuzzerConfig configStatus = ApplicationViewModelRoot.Default.SystemClient.GetBuzzerConfig();
        Enable = (byte)(configStatus.IsEnabled ? 1 : 0);
        Frequency = (uint)configStatus.FrequencyInHz;
        VolumePercent = (byte)configStatus.VolumePct;
    }
}
