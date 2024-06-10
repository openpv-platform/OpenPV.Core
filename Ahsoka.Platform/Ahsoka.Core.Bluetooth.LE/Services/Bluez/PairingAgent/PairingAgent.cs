using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluez;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez;

/// <summary>
/// Custom Pairing Agent for Managing Device Pairing Interactions
/// </summary>
class PairingAgent : IAgent
{
    readonly LinuxImplementation service;
    readonly AutoResetEvent waitForPairingConfirmation = new(false);
    bool pairingResult = false;
    readonly int defaultPairingWaitInMs = 60000; //60s

    public ObjectPath ObjectPath { get; init; }

    public PairingAgent(LinuxImplementation service, Connection connection)
    {
        ObjectPath = new ObjectPath($"/ahsoka/pairing_agent");
        this.service = service;

        RegisterAgent(connection);
    }

    /// <summary>
    ///  Register Agent with System and Make Default System Agent
    /// </summary>
    /// <param name="connection"></param>
    internal void RegisterAgent(Connection connection)
    {
        var agentManager = connection.CreateProxy<IAgentManager1>("org.bluez", "/org/bluez");
        if (agentManager != null)
        {
            connection.RegisterObjectAsync(this).GetAwaiter().GetResult();
            agentManager.RegisterAgentAsync(this.ObjectPath, "DisplayYesNo").GetAwaiter();
            agentManager.RequestDefaultAgentAsync(this.ObjectPath).GetAwaiter();
        }
    }

    /// <summary>
    /// This method gets called when the service daemon 
    /// unregisters the agent.An agent can use it to do
    /// cleanup tasks.There is no need to unregister the
    /// agent, because when this method gets called it has
    /// already been unregistered.
    /// </summary>
    public Task ReleaseAsync()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Pairing Agent: Released");
        return Task.CompletedTask;
    }

    /// <summary>
    /// This method gets called when the service daemon 
    /// needs to get the passkey for an authentication. 
    /// The return value should be a string of 1-16 characters length. 
    /// The string can be alphanumeric.
    ///
    /// Possible errors: org.bluez.Error.Rejected
    ///                 org.bluez.Error.Canceled
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public Task<string> RequestPinCodeAsync(ObjectPath device)
    {
        // Generate a 6 Digit Number Code
        int value = Random.Shared.Next(100000, 999999);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: PIN Generated : {value}");
        return Task.FromResult(value.ToString());
    }

    /// <summary>
    /// This method gets called when the service daemon
    /// needs to get the passkey for an authentication.
    /// The return value should be a numeric value
    /// between 0-999999.
    /// Possible errors: org.bluez.Error.Rejected
    ///                 org.bluez.Error.Canceled
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public Task<uint> RequestPasskeyAsync(ObjectPath device)
    {
        int value = Random.Shared.Next(100000, 999999);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: PassKey Generated : {value}");
        return Task.FromResult((uint)value);
    }


    /// <summary>
    ///  This method gets called when the service daemon
    ///  needs to display a pincode for an authentication.
    ///  An empty reply should be returned. When the pincode
    ///  needs no longer to be displayed, the Cancel method
    ///  of the agent will be called.
    ///
    ///  This is used during the pairing process of keyboards
    ///  that don't support Bluetooth 2.1 Secure Simple Pairing,
    ///  in contrast to DisplayPasskey which is used for those
    ///  that do.
    ///
    ///  This method will only ever be called once since
    ///  older keyboards do not support typing notification.
    ///
    ///  Note that the PIN will always be a 6-digit number,
    ///  zero-padded to 6 digits.This is for harmony with
    ///  the later specification.
    ///
    /// Possible errors: org.bluez.Error.Rejected
    ///                        org.bluez.Error.Canceled
    /// </summary>
    /// <param name="device"></param>
    /// <param name="pincode"></param>
    public Task DisplayPinCodeAsync(ObjectPath device, string pincode)
    {
        // Send Notification for Client to show a Pairing Dialog.
        service.Service.SendNotification(BLEMessageTypes.Ids.PairingRequested, new BLEPairingRequest()
        {
            Passcode = pincode,
            RequestType = BLEPairingRequestType.DisplayUntilCancelled
        });

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Display PIN Requested: {pincode}");
        return Task.FromResult(String.Empty);
    }


    /// <summary>
    /// This method gets called when the service daemon
    /// needs to display a passkey for an authentication.
    /// 
    /// The entered parameter indicates the number of already
    /// typed keys on the remote side.
    /// 
    /// An empty reply should be returned. When the passkey
    /// needs no longer to be displayed, the Cancel method
    /// of the agent will be called.
    /// 
    /// During the pairing process this method might be
    /// called multiple times to update the entered value.
    /// 
    /// Note that the passkey will always be a 6-digit number,
    /// so the display should be zero-padded at the start if
    /// the value contains less than 6 digits.
    /// 
    /// </summary>
    /// <param name="device"></param>
    /// <param name="passkey"></param>
    /// <param name="entered"></param>
    public Task DisplayPasskeyAsync(ObjectPath device, uint passkey, ushort entered)
    {
        // Send Notification for Client to show a Pairing Dialog.
        service.Service.SendNotification(BLEMessageTypes.Ids.PairingRequested, new BLEPairingRequest()
        {
            Passcode = passkey.ToString("D6"), // Padded to 6
            RequestType = BLEPairingRequestType.DisplayUntilCancelled
        });

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Display PassKey Requested : {passkey}");
        return Task.FromResult(String.Empty);
    }

    /// <summary>
    /// This method gets called when the service daemon
    /// needs to confirm a passkey for an authentication.
    /// 
    /// To confirm the value it should return an empty reply
    /// or an error in case the passkey is invalid.
    /// 
    /// Note that the passkey will always be a 6-digit number,
    /// so the display should be zero-padded at the start if
    /// the value contains less than 6 digits.
    /// 
    /// Possible errors: org.bluez.Error.Rejected
    ///                  org.bluez.Error.Canceled
    /// </summary>
    /// <param name="device"></param>
    /// <param name="passkey"></param>
    public Task RequestConfirmationAsync(ObjectPath device, uint passkey)
    {
        // Send Notification for Client to show a Pairing Dialog.
        service.Service.SendNotification(BLEMessageTypes.Ids.PairingRequested, new BLEPairingRequest()
        {
            Passcode = passkey.ToString("D6"), // Padded to 6
            RequestType = BLEPairingRequestType.ConfirmValue // CLient must Confirm / Reject
        });

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Confirm PassKey : {passkey}");

        // Wait for Result
        pairingResult = false;
        waitForPairingConfirmation.WaitOne(defaultPairingWaitInMs);

        if (pairingResult)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: PassKey Confirmed : {passkey}");
            return Task.FromResult(String.Empty);
        }
        else
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: PassKey Rejected : {passkey}");
            return Task.FromException(new DBusException("org.bluez.Error.Rejected", "Passkey Rejected"));
        }
    }


    /// <summary>
    /// This method gets called to request the user to
    /// authorize an incoming pairing attempt which
    /// would in other circumstances trigger the just-works
    /// model.
    /// Possible errors: org.bluez.Error.Rejected
    ///                  org.bluez.Error.Canceled
    /// </summary>
    /// <param name="device"></param>
    public Task RequestAuthorizationAsync(ObjectPath device)
    {
        // Send Notification for Client to show a Pairing Dialog.
        service.Service.SendNotification(BLEMessageTypes.Ids.PairingRequested, new BLEPairingRequest()
        {
            Passcode = "",
            RequestType = BLEPairingRequestType.ConfirmNoValue // Client must Confirm / Reject
        });
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Confirm Authorization");

        // Wait for Result
        pairingResult = false;
        waitForPairingConfirmation.WaitOne(defaultPairingWaitInMs);

        if (pairingResult)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Authorization Confirmed");
            return Task.FromResult(String.Empty);
        }
        else
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Authorization Rejected");
            return Task.FromException(new DBusException("org.bluez.Error.Rejected", "Passkey Rejected"));
        }
    }

    /// <summary>
    /// This method gets called when the service daemon
    /// needs to authorize a connection/service request.
    ///
    /// Possible errors: org.bluez.Error.Rejected
    ///                 org.bluez.Error.Canceled
    /// </summary>
    /// <param name="device"></param>
    /// <param name="uuid"></param>
    public Task AuthorizeServiceAsync(ObjectPath device, string uuid)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Service Authorization Requested: {device} {uuid}");
        return Task.FromResult(String.Empty);
    }

    /// <summary>
    /// This method gets called to indicate that the agent
    /// request failed before a reply was returned.
    /// </summary>
    public Task CancelAsync()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Cancelled");
        return Task.FromResult(String.Empty);
    }

    /// <summary>
    /// Release a Pairing Request
    /// </summary>
    /// <param name="confirmation"></param>
    public void ConfirmPairingRequest(BLEPairingConfirmation confirmation)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Pairing Agent: Confirmation Receieved {confirmation.Confirmed}");
        this.pairingResult = confirmation.Confirmed;
        this.waitForPairingConfirmation.Set();
    }

    /// <summary>
    /// Dispose any Resources
    /// </summary>
    void IDisposable.Dispose()
    {

    }
}
