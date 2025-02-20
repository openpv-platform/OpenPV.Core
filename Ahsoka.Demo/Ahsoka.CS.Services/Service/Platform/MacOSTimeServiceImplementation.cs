using Ahsoka.Core.Utility;
using System;

namespace Ahsoka.Services.Demo.Service.Platform;


/// <summary>
/// This class handles the ITimeService Implementation for Linux
/// </summary>
internal class MacOSTimeServiceImplementation : ITimeServiceImplemntation
{
    /// <summary>
    ///  Use Cross Platform calls to handle the GetTime
    /// </summary>
    /// <returns></returns>
    public TimeInfo GetTime()
    {
        return new TimeInfo()
        {
            CurrentTime = DateTime.Now,
            TimeZone = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Hours
        };
    }

    /// <summary>
    /// Handle the Platform Specific Method for Setting the Date.
    /// </summary>
    /// <param name="timeInfo"></param>
    /// <returns></returns>
    public SetTimeResult SetTime(TimeInfo timeInfo)
    {
        // Set the Unix System Clock to the Time.
        DateTimeOffset offset = timeInfo.CurrentTime.Value;
        ProcessUtility.RunProcess("date", "--set @" + offset.ToUnixTimeSeconds().ToString(), null, out string _, out string error);

        var newTime = GetTime();
        return new SetTimeResult() { CurrentTime = newTime.CurrentTime.Value, Success = String.IsNullOrEmpty(error), TimeZone = timeInfo.TimeZone };
    }
}