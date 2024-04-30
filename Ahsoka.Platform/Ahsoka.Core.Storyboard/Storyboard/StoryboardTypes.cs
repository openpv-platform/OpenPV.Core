using System;
using System.Collections.Generic;
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
/// A set of string enumerations that define the direction of the Storyboard messages
/// </summary>
public enum StoryboardMessageDirection
{
    /// <summary>
    /// The Storyboard Message is coming into the C# application.
    /// </summary>
    In,
    /// <summary>
    /// The Storyboard Message is coming out from the C# application.
    /// </summary>
    Out,
}

/// <summary>
/// A static class for containing extension methods
/// </summary>
public static class StoryboardMessageExtensions
{
    /// <summary>
    /// Utility function used to Extract a Type Safe Object from SBIO
    /// </summary>
    internal static string GetMessageDirectionString(this StoryboardMessageDirection directionEnum)
    {
        return directionEnum switch
        {
            StoryboardMessageDirection.In => "0x20",
            StoryboardMessageDirection.Out => "0x40",
            _ => throw new Exception("The direction provided isn't supported."),
        };
    }

}

