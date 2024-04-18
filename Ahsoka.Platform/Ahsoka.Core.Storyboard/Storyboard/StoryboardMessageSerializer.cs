using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ahsoka.Storyboard;

class StoryboardMessageSerializer
{
    #region Static
    static readonly Dictionary<Type, StoryboardMessageSerializer> eventMap = new();
    static readonly Dictionary<string, StoryboardMessageSerializer> nameMap = new();
    static readonly Dictionary<Type, SBFieldType> typeTranslation = new();
    static readonly object syncRoot = new();

    static StoryboardMessageSerializer()
    {
        lock (syncRoot)
        {
            // Build Type Map
            typeTranslation.Add(typeof(float), SBFieldType.Float_4f);
            typeTranslation.Add(typeof(UInt64), SBFieldType.Int64_8u);
            typeTranslation.Add(typeof(UInt32), SBFieldType.Int32_4u);
            typeTranslation.Add(typeof(UInt16), SBFieldType.Int16_2u);
            typeTranslation.Add(typeof(byte), SBFieldType.Int8_1u);
            typeTranslation.Add(typeof(Int64), SBFieldType.SignedInt64_8s);
            typeTranslation.Add(typeof(Int32), SBFieldType.SignedInt32_4s);
            typeTranslation.Add(typeof(Int16), SBFieldType.SignedInt16_2s);
            typeTranslation.Add(typeof(sbyte), SBFieldType.SignedInt8_1s);
            typeTranslation.Add(typeof(string), SBFieldType.String_1s);

            foreach (var item in ClassLoader.GetItemsWithAttribute<SBMessageEventName>())
            {
                // Load Message Objects
                var attr = item.Key;
                attr.EventName ??= item.Value.Name;
                attr.EventDirection ??= StoryboardMessageDirection.Out.GetMessageDirectionString();

                // Construct and Cache Parsing INfo
                StoryboardMessageSerializer sbEventHandler = new(item.Value, attr);
                eventMap.Add(item.Value, sbEventHandler);
                nameMap.Add(attr.EventName, sbEventHandler);
            }
        }
    }

    public static StoryboardMessageSerializer GetMessageHandler(Type messageType)
    {
        eventMap.TryGetValue(messageType, out StoryboardMessageSerializer handler);
        return handler;
    }

    public static StoryboardMessageSerializer GetMessageHandler(string messageName)
    {
        nameMap.TryGetValue(messageName, out StoryboardMessageSerializer handler);
        return handler;
    }
    #endregion

    #region Properties
    public SBMessageEventName EventAttribute { get; private set; }

    public Type Type { get; private set; }

    public string Format { get; private set; }

    public static Dictionary<Type, SBFieldType> TypeTranslation => typeTranslation;

    private readonly List<KeyValuePair<PropertyInfo, SBFieldAttribute>> propertyData = new();
    #endregion

    #region Methods
    private StoryboardMessageSerializer(Type type, SBMessageEventName eventAttr)
    {
        this.Type = type;

        // Gather Property Data
        this.EventAttribute = eventAttr;

        // Gather Info about Fields
        foreach (var item in type.GetProperties())
        {
            var attribute = item.GetCustomAttribute<SBFieldAttribute>();
            if (attribute != null)
            {
                // Update Data Translation Data
                attribute.FieldType = GetSBType(item.PropertyType);
                attribute.Count = attribute.FieldType == SBFieldType.String_1s ? 0 : attribute.Count;

                // Capture Info.
                propertyData.Add(new KeyValuePair<PropertyInfo, SBFieldAttribute>(item, attribute));
            }
        }

        // Store and Sort Runtime Data
        propertyData.Sort((a, b) => (a.Value.Ordinal).CompareTo(b.Value.Ordinal));

        Format = string.Join(" ", propertyData.Select(x => $"{x.Value.FieldType.GetDescription()}{x.Value.Count} {x.Value.FieldName}"));
    }

  
    private SBFieldType GetSBType(Type propertyType)
    {
        if (propertyType.IsArray)
            propertyType = propertyType.GetElementType();

        if (!typeTranslation.TryGetValue(propertyType, out SBFieldType value))
            throw new ApplicationException("Could not determine SBField Type for Object");

        return value;
    }
    #endregion

    #region Serialize
    public byte[] ObjectToData(object messageToSerialize)
    {
        List<byte> bytes = new();
        foreach (var prop in propertyData)
        {
            var propVal = prop.Key.GetValue(messageToSerialize);
            if (propVal != null)
            {
                if (prop.Value.Count <= 1) // Covers String
                    AddBytes(bytes, prop.Value.FieldType, propVal);
                else if (prop.Value.Count > 1)
                    AddBytesArray(bytes, prop.Value.FieldType, prop.Value.Count, propVal as Array);
            }
        }

        return bytes.ToArray();
    }

    private static void AddBytesArray(List<byte> bytes, SBFieldType fieldType, int count, Array value)
    {
        for (int i = 0; i < count; i++)
            AddBytes(bytes, fieldType, value.GetValue(i));
    }

    private static void AddBytes(List<byte> bytes, SBFieldType fieldtype, object propVal)
    {
        switch (fieldtype)
        {
            case SBFieldType.Float_4f:
                bytes.AddRange(BitConverter.GetBytes((float)propVal));
                break;
            case SBFieldType.Int64_8u:
                bytes.AddRange(BitConverter.GetBytes((UInt64)propVal));
                break;
            case SBFieldType.Int32_4u:
                bytes.AddRange(BitConverter.GetBytes((UInt32)propVal));
                break;
            case SBFieldType.Int16_2u:
                bytes.AddRange(BitConverter.GetBytes((UInt16)propVal));
                break;
            case SBFieldType.Int8_1u:
                bytes.Add((byte)propVal);
                break;
            case SBFieldType.SignedInt64_8s:
                bytes.AddRange(BitConverter.GetBytes((Int64)propVal));
                break;
            case SBFieldType.SignedInt32_4s:
                bytes.AddRange(BitConverter.GetBytes((Int32)propVal));
                break;
            case SBFieldType.SignedInt16_2s:
                bytes.AddRange(BitConverter.GetBytes((Int16)propVal));
                break;
            case SBFieldType.SignedInt8_1s:
                bytes.Add((Byte)Convert.ToSByte(propVal));
                break;
            case SBFieldType.String_1s:
                bytes.AddRange(UTF8Encoding.UTF8.GetBytes((string)propVal));
                bytes.Add(0); // Null Terminated Strings
                break;
        }
    }
    #endregion

    #region Deserialize
    public object DataToObject(byte[] data)
    {
        object returnObj = Activator.CreateInstance(this.Type);
        int position = 0;
        foreach (var prop in propertyData)
        {
            object value = null;

            if (prop.Value.Count <= 1) // Covers String
                value = GetValue(data, ref position, prop.Value.FieldType);
            else if (prop.Value.Count > 1)
                value = GetValueArray(data, ref position, prop.Value.FieldType, prop.Value.Count, prop.Key.PropertyType.GetElementType());

            prop.Key.SetValue(returnObj, value);
        }

        return returnObj;
    }

    private object GetValueArray(byte[] bytes, ref int position, SBFieldType fieldType, int count, Type propType)
    {
        Array arrayValue = Array.CreateInstance(propType, count);
        for (int i = 0; i < count; i++)
            arrayValue.SetValue(GetValue(bytes, ref position, fieldType), i);

        return arrayValue;
    }

    private object GetValue(byte[] bytes, ref int position, SBFieldType fieldType)
    {
        object returnValue = null;
        switch (fieldType)
        {
            case SBFieldType.Float_4f:
                returnValue = BitConverter.ToSingle(bytes, position);
                position += 4;
                break;
            case SBFieldType.Int64_8u:
                returnValue = BitConverter.ToUInt64(bytes, position);
                position += 8;
                break;
            case SBFieldType.Int32_4u:
                returnValue = BitConverter.ToUInt32(bytes, position);
                position += 4;
                break;
            case SBFieldType.Int16_2u:
                returnValue = BitConverter.ToUInt16(bytes, position);
                position += 2;
                break;
            case SBFieldType.Int8_1u:
                returnValue = bytes[position];
                position++;
                break;
            case SBFieldType.SignedInt64_8s:
                returnValue = BitConverter.ToInt64(bytes, position);
                position += 8;
                break;
            case SBFieldType.SignedInt32_4s:
                returnValue = BitConverter.ToInt32(bytes, position);
                position += 4;
                break;
            case SBFieldType.SignedInt16_2s:
                returnValue = BitConverter.ToInt16(bytes, position);
                position += 2;
                break;
            case SBFieldType.SignedInt8_1s:
                returnValue = (sbyte)bytes[position];
                position++;
                break;
            case SBFieldType.String_1s:
                returnValue = UTF8Encoding.UTF8.GetString(bytes, position, bytes.Length - position - 1); // Strings are always last item
                position = bytes.Length; // So we know we are done 
                break;
        }

        return returnValue;
    }
    #endregion
}
