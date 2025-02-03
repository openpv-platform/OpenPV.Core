using Ahsoka.Core.Utility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class LinuxSystemUtilities
{
    internal static void Reboot()
    {
        Console.WriteLine($"/sbin/reboot");
        ProcessUtility.RunProcess("/sbin/reboot", null, null, out string log, out string error);
    }
}