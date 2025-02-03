using System;

namespace Ahsoka.Core;

/// <summary>
/// Extensions for Ahsoka Sockets
/// </summary>
public static class AhsokaSocketExtensions
{
    /// <summary>
    /// Guid Convertion
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ToGuidString(this byte[] bytes)
    {
        return bytes.ToGuid().ToString();
    }

    /// <summary>
    /// Guid Convertion
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Guid ToGuid(this byte[] bytes)
    {
        return new Guid(bytes);
    }
}

