using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ahsoka.Storyboard;

/// <summary>
/// Info for a Single Property
/// </summary>
public class StoryboardPropertyInfo
{
    /// <summary>
    /// Map of Storyboard Types to Dotnet Types
    /// </summary>
    public static Dictionary<Type, SBFieldType> TypeTranslation { get; private set; } = new() { 
        { typeof(float), SBFieldType.Float_4f },
        { typeof(UInt64), SBFieldType.Int64_8u },
        { typeof(UInt32), SBFieldType.Int32_4u },
        { typeof(UInt16), SBFieldType.Int16_2u },
        { typeof(byte), SBFieldType.Int8_1u },
        { typeof(Int64), SBFieldType.SignedInt64_8s },
        { typeof(Int32), SBFieldType.SignedInt32_4s },
        { typeof(Int16), SBFieldType.SignedInt16_2s },
        { typeof(sbyte), SBFieldType.SignedInt8_1s },
        { typeof(string), SBFieldType.String_1s }
    };

    /// <summary>
    /// Order of this event property in SBIO
    /// </summary>
    public int Ordinal { get; private set; }

    /// <summary>
    /// The Size of the Field in SBIO (For Arrays)
    /// </summary>
    public int Count { get; internal set; }

    /// <summary>
    /// Name of the Field
    /// </summary>
    public string FieldName { get; private set; }

    /// <summary>
    /// Type of Field
    /// </summary>
    public SBFieldType FieldType { get; internal set; }

    /// <summary>
    /// Type of Field
    /// </summary>
    public int StartByte { get; internal set; }

    /// <summary>
    /// Type of Field
    /// </summary>
    public int ElementSize { get; internal set; }

    /// <summary>
    /// Constructor used to contruct DotNet Attribute.
    /// </summary>
    /// <param name="ordinal"></param>
    /// <param name="fieldType"></param>
    /// <param name="startByte"></param>
    /// <param name="numBytes"></param>
    /// <param name="count"></param>
    /// <param name="fieldName"></param>
    public StoryboardPropertyInfo(int ordinal, SBFieldType fieldType, int startByte, int numBytes, int count = 1, [CallerMemberName] string fieldName = null)
    {
        Ordinal = ordinal;
        FieldType = fieldType;
        StartByte = startByte;
        ElementSize = numBytes;

        if (fieldName == null)
            FieldName = $"variable{ordinal}";
        else
            FieldName = fieldName;

        Count = count;
    }

    /// <summary>
    /// Sets data to the Data Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="newValue"></param>
    public void SetValue<T>(ref byte[] data, T newValue)
    {
        if (Count <= 1) // Covers String
            SetBytes(ref data, StartByte, FieldType, newValue);
        else if (Count > 1)
            SetBytesArray(data, StartByte, ElementSize, FieldType, Count, newValue as Array);
    }

    /// <summary>
    /// Get Value Converted from Bytes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    public T GetValue<T>(byte[] data)
    {
        if (Count <= 1) // Covers String
            return (T)GetBytes(data.ToArray(), StartByte, FieldType);
        else
            return (T)GetBytesArray(data.ToArray(), StartByte, FieldType, Count, TypeTranslation.First(x => x.Value == FieldType).Key);
    }

    private object GetBytesArray(byte[] bytes, int position, SBFieldType fieldType, int count, Type propType)
    {
        Array arrayValue = Array.CreateInstance(propType, count);
        for (int i = 0; i < count; i++)
            arrayValue.SetValue(GetBytes(bytes, position + i * ElementSize, fieldType), i);

        return arrayValue;
    }

    private void SetBytesArray(byte[] bytes, int position, int numBytes, SBFieldType fieldType, int count, Array value)
    {
        for (int i = 0; i < count; i++)
            SetBytes(ref bytes, position + i * numBytes, fieldType, value.GetValue(i));
    }

    private void SetBytes(ref byte[] bytes, int position, SBFieldType fieldtype, object propVal)
    {
        switch (fieldtype)
        {
            case SBFieldType.Float_4f:
                Array.Copy(BitConverter.GetBytes((float)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.Int64_8u:
                Array.Copy(BitConverter.GetBytes((UInt64)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.Int32_4u:
                Array.Copy(BitConverter.GetBytes((UInt32)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.Int16_2u:
                Array.Copy(BitConverter.GetBytes((UInt16)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.Int8_1u:
                bytes[position] = (byte)propVal;
                break;
            case SBFieldType.SignedInt64_8s:
                Array.Copy(BitConverter.GetBytes((Int64)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.SignedInt32_4s:
                Array.Copy(BitConverter.GetBytes((Int32)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.SignedInt16_2s:
                Array.Copy(BitConverter.GetBytes((Int16)propVal), 0, bytes, position, ElementSize);
                break;
            case SBFieldType.SignedInt8_1s:
                bytes[position] = (Byte)Convert.ToSByte(propVal);
                break;
            case SBFieldType.String_1s:
                var start = bytes.Take(position).ToArray();
                var newBytes = Encoding.UTF8.GetBytes((string)propVal + "\0");
                bytes = new byte[start.Length + newBytes.Length];
                Array.Copy(start, 0, bytes, 0, start.Length);
                Array.Copy(newBytes, 0, bytes, start.Length, newBytes.Length);
                break;
        }
    }

    private object GetBytes(byte[] bytes, int position, SBFieldType fieldType)
    {
        object returnValue = null;
        switch (fieldType)
        {
            case SBFieldType.Float_4f:
                returnValue = BitConverter.ToSingle(bytes, position);
                break;
            case SBFieldType.Int64_8u:
                returnValue = BitConverter.ToUInt64(bytes, position);
                break;
            case SBFieldType.Int32_4u:
                returnValue = BitConverter.ToUInt32(bytes, position);
                break;
            case SBFieldType.Int16_2u:
                returnValue = BitConverter.ToUInt16(bytes, position);
                break;
            case SBFieldType.Int8_1u:
                returnValue = bytes[position];
                break;
            case SBFieldType.SignedInt64_8s:
                returnValue = BitConverter.ToInt64(bytes, position);
                break;
            case SBFieldType.SignedInt32_4s:
                returnValue = BitConverter.ToInt32(bytes, position);
                break;
            case SBFieldType.SignedInt16_2s:
                returnValue = BitConverter.ToInt16(bytes, position);
                break;
            case SBFieldType.SignedInt8_1s:
                returnValue = (sbyte)bytes[position];
                break;
            case SBFieldType.String_1s:
                returnValue = UTF8Encoding.UTF8.GetString(bytes, position, bytes.Length - position - 1); // Strings are always last item
                break;
        }

        return returnValue;
    }
}
