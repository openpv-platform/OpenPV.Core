using Ahsoka.Core;
using Ahsoka.DeveloperTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SystemConsole = System.Console;

namespace Ahsoka.DeveloperTools.ViewModel;

public class LocalCommandLine()
{
    public static void RunWithLocalConsole(string processArgs, string workingDirectory, IProgress<CommandLineProgressInfo> progressString)
    {
        var currentDirectory = Environment.CurrentDirectory;
        if (workingDirectory != null)
            Environment.CurrentDirectory = workingDirectory;

        using var localConsole = new LocalConsoleWriter();

        EventHandler<LocalConsoleWriterEventArgs> handler = (sender, e) =>
        {
            progressString.Report(new CommandLineProgressInfo() { IsWriteLine = e.IsWriteLine, Message = e.Value });
        };

        localConsole.WriteLineEvent += handler;
        localConsole.WriteEvent += handler;

        SystemConsole.SetOut(localConsole);

        /// Split Arguments
        var args = new List<string>();
        args.AddRange(Regex.Matches(processArgs, @"[\""].+?[\""]|[^ ]+")
                        .Cast<Match>()
                        .Select(m => m.Value.Trim('"'))
                        .ToList());

        CommandLine.Execute(args.ToArray());

        localConsole.WriteLineEvent -= handler;
        localConsole.WriteEvent += handler;
        if (workingDirectory != null)
            Environment.CurrentDirectory = currentDirectory;
    }
}

public class LocalConsoleWriterEventArgs : EventArgs
{
    public string Value { get; private set; }
    public bool IsWriteLine { get; private set; }
    public LocalConsoleWriterEventArgs(string value, bool isWriteLine)
    {
        Value = value;
        IsWriteLine = isWriteLine;
    }
}

public class LocalConsoleWriter : TextWriter
{
    bool isWriteLine = false;
    public override Encoding Encoding { get { return Encoding.UTF8; } }

    public override void Write(string value)
    {
        if (WriteEvent != null && !isWriteLine)
            WriteEvent(this, new LocalConsoleWriterEventArgs(value, false));

        base.Write(value);
    }

    public override void WriteLine()
    {
        WriteLineEvent?.Invoke(this, new LocalConsoleWriterEventArgs("", true));

        base.WriteLine();
    }

    public override void WriteLine(string value)
    {
        isWriteLine = true;

        WriteLineEvent?.Invoke(this, new LocalConsoleWriterEventArgs(value, true));

        base.WriteLine(value);

        isWriteLine = false;
    }

    public event EventHandler<LocalConsoleWriterEventArgs> WriteEvent;
    public event EventHandler<LocalConsoleWriterEventArgs> WriteLineEvent;
}
