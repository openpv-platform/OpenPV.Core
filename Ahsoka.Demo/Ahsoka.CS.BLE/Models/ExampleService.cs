using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.LE;

namespace Ahsoka.CS.BLE.Models;

/// <summary>
/// This class demonstrates how to create a model based BLE Service.
/// By inheriting from LE.ServiceViewModelBase, we can define 
/// our service largely using attributes on the class (service) and 
/// properties (characteristics).  Passing this object to the Ahsoka BLE
/// service will register the Model as a BLE Service in Ahsoka Architedture 
/// and handle the sets / gets of the various descriptors.  
/// 
/// Note that the method "HandleWriteRequest" is raised if the phone updates a descriptor.  From here
/// we can choose to accept the descriptor or not or extend any property change 
/// behaviors needed by our system (update a UI for example).   The default 
/// implementation will accept the value if it's UUID matches however more 
/// advanced validation could be implemented here as well.
/// 
/// This class also demonstrates how to decorate the class with advert data such 
/// as (manufacturer data) to add values to the Advert.
/// </summary>
[BLEService("ExampleService", ServiceUUID)]
[BLEManufactureData(0xFFFE, 0xFF, 0xFF, 0xFF, 0xFF)]
public class ExampleService : BLEServiceViewModelBase
{
    #region Fields
    private const string ServiceUUID = "78e4de00-1e75-4032-a619-4d97982a479d";
    private const string StringValueUUID = "78e4de01-1e75-4032-a619-4d97982a479d";
    private const string ByteValueUUID = "78e4de02-1e75-4032-a619-4d97982a479d";

    string stringValue = string.Empty;
    byte[] byteValue = null;
    #endregion

    #region Properties
    /// <summary>
    /// Example of an Encrypted Read / Write Values (Notify is not Encrypted but can be with the CCCD Descriptor)
    /// </summary>
    [BLECharacteristic("StringValue", StringValueUUID,
        BLECharacteristicFlag.EncryptRead, BLECharacteristicFlag.EncryptWrite, BLECharacteristicFlag.Notify)]
    public string StringValue
    {
        get { return stringValue; }
        set { OnSetObject(ref stringValue, value); }
    }

    [BLECharacteristic("ByteValue",
        ByteValueUUID, BLECharacteristicFlag.Read, BLECharacteristicFlag.Write)]
    public byte[] ByteValue
    {
        get { return byteValue; }
        set { OnSetObject(ref byteValue, value); }
    }
    #endregion

    #region Methods
    public ExampleService()
    {
        StringValue = "MyStartValue";
        ByteValue = new byte[4] { 0, 1, 2, 3 };
    }

    /// <summary>
    /// This Method is called to Notify The Model that the Property Was Changed
    /// </summary>
    /// <param name="updateNotification"></param>
    protected override void OnHandleWriteRequest(BLECharacteristicValue updateNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"APP: Characteristic Written {updateNotification.Uuid}");
    }

    #endregion
}
