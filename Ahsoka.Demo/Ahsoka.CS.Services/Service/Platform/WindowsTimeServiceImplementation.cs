using Ahsoka.ServiceFramework;
using System;

namespace Ahsoka.Services.Demo.Service.Platform;

/// <summary>
/// Handle Time Service on Windows
/// Adding this allows you to simulate your application service on Windows
/// </summary>
internal class WindowsTimeServiceImplementation : ITimeServiceImplemntation
{
    /// <summary>
    /// Get Time using Crosss Platform DotNetCore Time Functions.
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
    /// Simulation of Set Time (does not set time)
    /// </summary>
    /// <param name="timeInfo">The new time to set</param>
    /// <returns></returns>
    public SetTimeResult SetTime(TimeInfo timeInfo)
    {
        // Note that we don't actually change the time here.
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Pretending to Set the Clock to New Time");
        return new SetTimeResult() { CurrentTime = timeInfo.CurrentTime.Value, Success = true, TimeZone = timeInfo.TimeZone };
    }
}