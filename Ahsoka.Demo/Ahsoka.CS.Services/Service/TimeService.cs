using Ahsoka.ServiceFramework;
using Ahsoka.Services.Demo.Service;
using Ahsoka.Services.Demo.Service.Platform;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Threading.Tasks;

namespace Ahsoka.Services.Demo;

/// <summary>
/// This is the implementation of our Time Service.
/// It demostrates how to handle messages and return results using async and sync style methods.
/// It also demostrates a simple method of selecting a platform specific implemntation of the service
/// for windows and linux.
/// </summary>
public class TimeService : AhsokaTCPServiceBase<TimeServiceMessageTypes.Ids>
{
    #region Fields
    readonly ITimeServiceImplemntation timeServiceImplemntation;
    public const string Name = "TimeService";
    #endregion

    #region Methods
    /// <summary>
    ///  Base Constructor that uses Default Configuration
    /// </summary>
    public TimeService() :
        base(TimeServiceConfig.Default, new TimeServiceMessages())
    {
        // Here we are simulating a multi-platform service so we will create an object to handle our calls 
        // based on the sel
        timeServiceImplemntation = SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WindowsTimeServiceImplementation(),
            PlatformFamily.OpenViewLinux => new LinuxTimeServiceImplementation(),
            PlatformFamily.MacosArm64 => new MacOSTimeServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    /// <summary>
    /// This is the Main Handler method for Ahsoka Services.    The message header 
    /// contains the Transport ID (Message Type) that allows us to switch between various handlers.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<TimeServiceMessageTypes.Ids>())
        {
            case TimeServiceMessageTypes.Ids.GetTime:

                HandleGetTime(messageHeader);
                break;

            case TimeServiceMessageTypes.Ids.SetTime:

                HandleSetTime(messageHeader, message);
                break;
        }
    }

    /// <summary>
    /// Handle the Set Time Message
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    private void HandleSetTime(AhsokaMessageHeader messageHeader, object message)
    {
        // Create a Thread for the Set Time Method
        // here we will send a notification when the time is set.
        Task.Run(() =>
        {
            SetTimeResult result = timeServiceImplemntation.SetTime(message as TimeInfo);

            // Create a Header for our Notification.
            var notifHeader = new AhsokaMessageHeader() { TransportId = TimeServiceMessageTypes.Ids.SetTimeCompleteNotification.EnumToInt() };

            // Send the Result as a Notification (async socket)
            SendMessageInternal(notifHeader, result, true);
        });

        // While our Task is Running Async, we go ahead adn release the client
        // by sending the empty notification the original header.
        SendMessageInternal(messageHeader, new EmptyNotification());
    }

    /// <summary>
    /// Handle the Get Time Message
    /// </summary>
    /// <param name="messageHeader"></param>
    private void HandleGetTime(AhsokaMessageHeader messageHeader)
    {
        // Get the Result of our Time request from the Platform Implemetnation
        TimeInfo info = timeServiceImplemntation.GetTime();

        // Return the Info to the Client.   Note that we reply with the messageHeader
        // which ensures this call returns the the calling client since services can
        // handle multiple clients.
        SendMessageInternal(messageHeader, info);
    }


    #endregion
}


