using System;
using System.ComponentModel;
using System.Reflection;

namespace Ahsoka.Utility;

/// <summary>
/// Extension Methods for Enumerations
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Get a DescriptionAttribute value from the Enumeration if adorned.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerationValue"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetDescription<T>(this T enumerationValue)
        where T : struct
    {
        Type type = enumerationValue.GetType();
        if (!type.IsEnum)
        {
            throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
        }

        //Tries to find a DescriptionAttribute for a potential friendly name
        //for the enum
        MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
        if (memberInfo != null && memberInfo.Length > 0)
        {
            object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs != null && attrs.Length > 0)
            {
                //Pull out the description value
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }

        //If we have no description attribute, just return the ToString of the enum
        return enumerationValue.ToString();
    }

    /// <summary>
    /// Convert an Emum to the Equivelent Integer Value
    /// </summary>
    /// <typeparam name="K">Enumeration Type</typeparam>
    /// <param name="value">Enumeration Value</param>
    /// <returns></returns>
    public static int EnumToInt<K>(this K value) where K : Enum
    {
        return (int)(object)value;
    }

    /// <summary>
    /// Convert a Integer to the Specified Enumeration
    /// </summary>
    /// <typeparam name="K">Type of Enum</typeparam>
    /// <param name="value">Equivelent Integer Value</param>
    /// <returns>Resulting Enum</returns>
    public static K IntToEnum<K>(this int value)
    {
        return (K)(object)value;
    }


}
