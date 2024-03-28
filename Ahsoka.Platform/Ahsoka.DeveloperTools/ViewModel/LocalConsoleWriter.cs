using System;
using System.IO;
using System.Text;

namespace Ahsoka.DeveloperTools.ViewModel;

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
