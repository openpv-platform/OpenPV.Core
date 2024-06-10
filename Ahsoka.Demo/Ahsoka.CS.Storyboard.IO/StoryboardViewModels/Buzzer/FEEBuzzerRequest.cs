using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.Application.Storyboard;

public partial class FEEBuzzerRequest
{
    private static readonly uint FrequencyMin = 100;
    private static readonly uint FrequencyMax = 5000;

    private static readonly byte VolumeMin = 0;
    private static readonly byte VolumeMax = 100;

    public void Init()
    {
        Frequency = ApplicationViewModelRoot.Default.BuzzerModel.Frequency;
        VolumePercent = ApplicationViewModelRoot.Default.BuzzerModel.VolumePercent;
        Enable = ApplicationViewModelRoot.Default.BuzzerModel.Enable;
    }

    public override void OnUpdateModel(string target)
    {

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"C# Buzzer Request");

        //Bound requests
        uint frequencyRequest = Frequency;
        frequencyRequest = Math.Min(frequencyRequest, FrequencyMax);
        frequencyRequest = Math.Max(frequencyRequest, FrequencyMin);

        byte volumeRequest = VolumePercent;
        volumeRequest = Math.Min(volumeRequest, VolumeMax);
        volumeRequest = Math.Max(volumeRequest, VolumeMin);

        bool enable = Convert.ToBoolean(Enable);

        //Make request to the model
        BuzzerConfig buzzerRequest = new()
        {
            IsEnabled = enable,
            FrequencyInHz = (int)frequencyRequest,
            VolumePct = volumeRequest
        };
        ApplicationViewModelRoot.Default.SystemClient.SetBuzzerConfig(buzzerRequest);

        //Update the UI
        ApplicationViewModelRoot.Default.BuzzerModel.UpdateStoryboard();

    }
}
