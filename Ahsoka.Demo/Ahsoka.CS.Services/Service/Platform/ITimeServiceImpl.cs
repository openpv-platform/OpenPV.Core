namespace Ahsoka.Services.Demo.Service.Platform;

/// <summary>
/// Interface that allows cross platform implementations 
/// for the time service.
/// </summary>
internal interface ITimeServiceImplemntation
{
    // Get the Current Time
    TimeInfo GetTime();

    // Set the Time and Return the Result
    SetTimeResult SetTime(TimeInfo timeInfo);
}
