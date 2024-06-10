using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.LE;
using System;

namespace Ahsoka.CS.BLE.Models;

/// <summary>
/// Second Service Example.  Note that this one does NOT advertise.
/// </summary>
[BLEService("ExampleService2", ServiceUUID, true, false)]
public class ExampleService2 : BLEServiceViewModelBase
{
    #region Fields
    private const string ServiceUUID = "78e4df00-1e75-4032-a619-4d97982a479d";
    private const string StringValueUUID = "78e4df01-1e75-4032-a619-4d97982a479d";
    private const string ByteValueUUID = "78e4df02-1e75-4032-a619-4d97982a479d";
    private const string DataValueUUID = "78e4df03-1e75-4032-a619-4d97982a479d";

    string stringValue = string.Empty;
    byte[] byteValue = null;
    byte[] dataValue = null;
    #endregion

    #region Properties
    [BLECharacteristic("StringValue", StringValueUUID,
        BLECharacteristicFlag.Read, BLECharacteristicFlag.Write, BLECharacteristicFlag.Notify)]
    public string StringValue
    {
        get { return stringValue; }
        set { OnSetObject(ref stringValue, value); }
    }

    [BLECharacteristic("ByteValue", ByteValueUUID, BLECharacteristicFlag.Read, BLECharacteristicFlag.Write)]
    public byte[] ByteValue
    {
        get { return byteValue; }
        set { OnSetObject(ref byteValue, value); }
    }

    // Variable to show Last Read (Only used for UI)
    public byte[] DataValue
    {
        get { return dataValue; }
        set { OnSetObject(ref dataValue, value); }
    }

    #endregion

    #region Methods
    public ExampleService2()
    {
        StringValue = "MyStartValue2";
        ByteValue = new byte[4] { 3, 2, 1, 0 };
    }


    /// <summary>
    /// This is an example of  Characteristic that is handled by a Read Method
    /// instead of a Property.  Note that this and the Write Methods will be combined
    /// into a single characteristic.
    /// </summary>
    /// <param name="updateNotification"></param>
    [BLECharacteristicReadMethod("DataValue", DataValueUUID, BLECharacteristicFlag.Read)]
    public void ReadDataValue(BLECharacteristicValue updateNotification)
    {
        int value = Random.Shared.Next(0, 99999);
        DataValue = updateNotification.Value = BitConverter.GetBytes(value);
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"APP: Read Data Returned for  {updateNotification.Uuid} with value {value}");
    }

    /// <summary>
    /// This is an example of  Characteristic that is handled by a Write Method
    /// instead of a Property.  Note that this and the Read Methods will be combined
    /// into a single characteristic.
    /// </summary>
    /// <param name="updateNotification"></param>
    [BLECharacteristicWriteMethod("DataValue", DataValueUUID, BLECharacteristicFlag.Write)]
    public void WriteDataValue(BLECharacteristicValue updateNotification)
    {
        DataValue = updateNotification.Value;
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"APP: Write Data Received for {updateNotification.Uuid}");
    }


    protected override void OnHandleWriteRequest(BLECharacteristicValue updateNotification)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"APP: Characteristic Written {updateNotification.Uuid}");
    }
    #endregion
}