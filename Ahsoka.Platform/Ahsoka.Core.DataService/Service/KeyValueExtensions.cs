using System;

namespace Ahsoka.Services.Data;

/// <summary>
/// Extension Methods for KeyValues
/// </summary>
public static class KeyValueExtensions
{
    #region Utility
    /// <summary>
    /// Gets a value from a KeyValue Object
    /// </summary>
    /// <param name="keyValue"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static object GetValue(this KeyValue keyValue)
    {
        if (keyValue.ShouldSerializeBooleanValue())
            return keyValue.BooleanValue;
        else if (keyValue.ShouldSerializeDataValue())
            return keyValue.DataValue;
        else if (keyValue.ShouldSerializeFloatValue())
            return keyValue.FloatValue;
        else if (keyValue.ShouldSerializeIntegerValue())
            return keyValue.IntegerValue;
        else if (keyValue.ShouldSerializeStringValue())
            return keyValue.StringValue;
        else if (keyValue.ShouldSerializeUnsignedIntegerValue())
            return keyValue.UnsignedIntegerValue;
        else
            throw new Exception($"Could not set value for {keyValue.KeyInfo.Origin} / {keyValue.KeyInfo.Key}");
    }

    /// <summary>
    /// Sets a value to a KeyValue Object
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void SetValue(this KeyValue keyValue, object value)
    {
        if (value.GetType() == typeof(bool))
            keyValue.BooleanValue = (bool)value;
        else if (value.GetType() == typeof(long) || value.GetType() == typeof(Int64))
            keyValue.IntegerValue = (long)value;
        else if (value.GetType() == typeof(int) || value.GetType() == typeof(Int32))
            keyValue.IntegerValue = (int)value;
        else if (value.GetType() == typeof(short) || value.GetType() == typeof(Int16))
            keyValue.IntegerValue = (short)value;
        else if (value.GetType() == typeof(sbyte))
            keyValue.IntegerValue = (sbyte)value;
        else if (value.GetType() == typeof(ulong) || value.GetType() == typeof(UInt64))
            keyValue.UnsignedIntegerValue = (ulong)value;
        else if (value.GetType() == typeof(uint) || value.GetType() == typeof(UInt32))
            keyValue.UnsignedIntegerValue = (uint)value;
        else if (value.GetType() == typeof(ushort) || value.GetType() == typeof(UInt16))
            keyValue.UnsignedIntegerValue = (ushort)value;
        else if (value.GetType() == typeof(byte))
            keyValue.UnsignedIntegerValue = (byte)value;
        else if (value.GetType() == typeof(float) || (value.GetType() == typeof(double)))
            keyValue.FloatValue = (double)value;
        else if (value.GetType() == typeof(byte[]))
            keyValue.DataValue = (byte[])value;
        else if (value.GetType() == typeof(string))
            keyValue.StringValue = (string)value;
        else { throw new ArgumentException($"{value.GetType()} Is Not Supported byt Data Service"); }
    }
    #endregion
}


/// <summary>
/// Info about a single key in the key service
/// </summary>
public partial class KeyInfo
{
    /// <summary>
    /// Create Key List from KeyInfo
    /// </summary>
    /// <returns></returns>
    public KeyList ToKeyList()
    {
        var kl = new KeyList();
        kl.Keys.Add(this);
        return kl;
    }
}

/// <summary>
/// Value Stored in the Global Data Service
/// </summary>
public partial class KeyValue
{
    /// <summary>
    /// Create Key Value List from KeyValue
    /// </summary>
    /// <returns></returns>
    public KeyValueList ToKeyValueList()
    {
        var kl = new KeyValueList();
        kl.Values.Add(this);
        return kl;
    }
}
