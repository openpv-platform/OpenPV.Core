using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ahsoka.Core.Utility;

internal static class StringUtilities
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }

    public static string GetByteFormattedSize(long len)
    {
        string[] sizes = { "b", "kb", "mb", "gb", "tb" };
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        return String.Format("{0:0.##}{1}", len, sizes[order]);
    }
}

/// <summary>
/// String Utility Function that are avaialble via C# Object Extension
/// </summary>
public static class StringExtensions
{
    static readonly string pattern = "(?![a-zA-Z0-9_]).";
    static readonly Regex regEx = new(pattern, RegexOptions.Compiled);

    /// <summary>
    /// Generates a validate Identifier from the given string
    /// </summary>
    /// <param name="Name"></param>
    /// <returns></returns>
    public static string AsIdentifier(this string Name)
    {
        if (string.IsNullOrEmpty(Name))
            return Name;

        string retVal = regEx.Replace(Name, "_");
        if (retVal.Length > 0 && Char.IsDigit(retVal[0]))
            retVal = "_" + retVal;

        return retVal.Replace("-", "Minus").Replace("+", "Plus").Replace("=", "Equals");
    }



    /// <summary>
    /// Splits a Camel Case String by Adding Spaces.
    /// </summary>
    /// <param name="str">String to split</param>
    /// <returns>String with Spaces Added</returns>
    public static string SplitCamelCase(this string str)
    {
        return Regex.Replace(
            Regex.Replace(
                str,
                @"(\P{Ll})(\P{Ll}\p{Ll})",
                "$1 $2"
            ),
            @"(\p{Ll})(\P{Ll})",
            "$1 $2"
        );
    }

    /// <summary>
    /// Remove the Last Line of Text from a String Builder.
    /// </summary>
    /// <param name="sb"></param>
    /// <returns></returns>
    public static StringBuilder RemoveToLastLine(this StringBuilder sb)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
        {
            if (Environment.NewLine.Contains(sb[i]))
            {
                sb.Remove(i, sb.Length - i);

                return sb;
            }
        }
        return sb;
    }

    /// <summary>
    /// Convert a string to Camel Case
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToCamelCase(this string str)
    {
        if (str.Contains("_"))
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLower().Replace("_", " ")).Replace(" ", "");
        else
            return str;

    }
}



