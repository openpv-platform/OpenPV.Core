using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ahsoka.Utility;

namespace Ahsoka.Storyboard;

/// <summary>
/// Field types used for the Storyboard SBIO Interface
/// </summary>
public enum SBFieldType
{
    /// <summary>
    /// 4 Byte Float Values
    /// </summary>
    [Description("4f")]
    Float_4f,

    /// <summary>
    /// Int64 Values
    /// </summary>
    [Description("8u")]
    Int64_8u,

    /// <summary>
    /// Int32 Values
    /// </summary>
    [Description("4u")]
    Int32_4u,

    /// <summary>
    /// Int16 Values
    /// </summary>
    [Description("2u")]
    Int16_2u,

    /// <summary>
    /// Int8 / Byte Values
    /// </summary>
    [Description("1u")]
    Int8_1u,

    /// <summary>
    /// Signed Int64 Values
    /// </summary>
    [Description("8s")]
    SignedInt64_8s,

    /// <summary>
    /// Signed Int32 Values
    /// </summary>
    [Description("4s")]
    SignedInt32_4s,

    /// <summary>
    /// Signed Int16 Values
    /// </summary>
    [Description("2s")]
    SignedInt16_2s,

    /// <summary>
    /// Signed Int8 Values
    /// </summary>
    [Description("1s")]
    SignedInt8_1s,

    /// <summary>
    /// String Values
    /// </summary>
    [Description("1s")]
    String_1s
}

/// <summary>
/// Attribute for Specifying the Name of a SBIO Event Object
/// should be used on an object representing an SBIO Event.
/// </summary>
public class SBMessageEventName : Attribute
{
    /// <summary>
    /// Name of the SBIO Event...should match Storyboard Event Name
    /// </summary>
    public string EventName { get; internal set; }

    /// <summary>
    /// Name of the Direction of the SBIO Event... Specifically if it is going to or from the Storyboard instance
    /// </summary>
    public string EventDirection {get; internal set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventDirection"></param>
    public SBMessageEventName(string eventName = null, string eventDirection = null)
    {
        EventName = eventName;
        EventDirection = eventDirection;
    }
}

/// <summary>
/// Attribute representing a SBIO Event Property
/// Describes information necessary to serialize messages in SBIO. 
/// </summary>
public class SBFieldAttribute : ViewModelPropertyBaseAttribute 
{
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
    /// Constructor used to contruct DotNet Attribute.
    /// </summary>
    /// <param name="ordinal"></param>
    /// <param name="count"></param>
    /// <param name="fieldName"></param>
    public SBFieldAttribute(int ordinal, int count = 1, [CallerMemberName] string fieldName = null)
    {
        Ordinal = ordinal;

        if (fieldName == null)
            FieldName = $"variable{ordinal}";
        else
            FieldName = fieldName;

        Count = count;
    }
}

