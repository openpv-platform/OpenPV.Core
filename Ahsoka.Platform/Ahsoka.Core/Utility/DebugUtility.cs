using System;
using System.Diagnostics;
using System.Threading;

namespace Ahsoka.Utility;

/// <summary>
///  Tool to assist in Debugging.
/// </summary>
public class DebugUtility
{
    /// <summary>
    /// Wait for the debugger to attach to this processes.
    /// </summary>
    public static void WaitForDebugger()
    {
        Console.WriteLine("Waiting for Debugger to Attach");
        while (!Debugger.IsAttached)
            Thread.Sleep(100);

        Console.WriteLine("Debugger Attached");
    }
}
