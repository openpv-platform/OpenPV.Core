using Ahsoka.System;
using Ahsoka.Utility;
using Avalonia.Threading;
using Renci.SshNet;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools.ViewModel;

internal class ConsoleViewModel : ChildViewModelBase<MainViewModel>, ICanCancel
{
    SshCommand asyncCommand;
    CancellationTokenSource cancelToken = null;
    readonly StringBuilder console = new();
    string latestString = string.Empty;
    bool showConsole = false;
    bool userOpened = false;

    internal string ConsoleOutput
    {
        get { return latestString; }
    }

    internal bool ConsoleVisible
    {
        get { return showConsole; }
        private set
        {
            showConsole = value;
            OnPropertyChanged();
        }
    }

    public ConsoleViewModel(MainViewModel parentViewModel) :
        base(parentViewModel)
    {
    }

    internal void ShowConsole(bool userShown = false)
    {
        ConsoleVisible = true;
        if (userShown)
            userOpened = userShown;
    }

    internal void HideConsole(bool userHidden = false)
    {
        if (userOpened == userHidden || userHidden)
        {
            ConsoleVisible = false;
            userOpened = false;
        }
    }

    // Hit X Button..Same as Hide User
    internal void ClosePopup()
    {
        HideConsole(true);
    }

    internal void ClearConsole()
    {
        console.Clear();
    }

    internal void CopyConsole()
    {
        var clipboard = ParentViewModel.RootVisual.Clipboard;
        clipboard.SetTextAsync(console.ToString());

    }

    internal void AddConsoleLine(string text)
    {
        ParentViewModel.StatusText = text;
        console.AppendLine(text);
        try { latestString = console.ToString(); }
        catch { };
        OnPropertyChanged(nameof(ConsoleOutput));

    }

    internal void AddConsoleCharacter(char text)
    {
        console.Append(text);
        latestString = console.ToString();
        OnPropertyChanged(nameof(ConsoleOutput));
    }

    internal void AddConsoleText(string text, bool removeLastLine = false)
    {
        if (removeLastLine)
            console.RemoveToLastLine();

        console.Append(text);
        latestString = console.ToString();
        OnPropertyChanged(nameof(ConsoleOutput));
    }

    internal void TrimLastLine()
    {
        while (console[console.Length - 1] != '\n')
            console.Remove(console.Length - 1, 1);
        latestString = console.ToString();
        OnPropertyChanged(nameof(ConsoleOutput));
    }

    internal void FetchInstallerJournal()
    {
        RunJournalCommandAsync("Installer", "Ahsoka.Installer", null);
    }

    internal void FetchApplicationJournal()
    {
        RunJournalCommandAsync("Application", "Ahsoka.Application", null);
    }

    private void RunJournalCommandAsync(string name, string serviceName, string watchForResult = null)
    {
        ClearConsole();
        AddConsoleLine($"Watching {name} Journal - Press Cancel To Stop Watching");
        AddConsoleLine("");

        ParentViewModel.StartProcess(this);
        Task.Run(() =>
        {
            return RunJournalCommand(serviceName, watchForResult);

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddConsoleLine("");
                ParentViewModel.FinishProcess($"Watch {name} Journal Ended", result.Status == TaskStatus.RanToCompletion);
            });
        });
    }

    public bool RunJournalCommand(string serviceName, params string[] watchForResult)
    {
        cancelToken = new CancellationTokenSource();

        using var sshClient = SshUtility.CreateSshClient(ParentViewModel.ActiveConnection.Connection);
        using (asyncCommand = sshClient.CreateCommand($"journalctl -u {serviceName}.service -f"))
        {
            var result = asyncCommand.BeginExecute();
            using var reader = new StreamReader(asyncCommand.OutputStream, Encoding.UTF8, true, 1024, true);
            while (!cancelToken.IsCancellationRequested && !result.IsCompleted)
            {
                try
                {
                    var line = reader.ReadLineAsync(cancelToken.Token).GetAwaiter().GetResult();
                    if (line != null)
                    {
                        AddConsoleLine(line);

                        if (watchForResult != null &&
                            watchForResult.Length > 0 &&
                            watchForResult.Any(s => s != null && line.Contains(s)))
                        {
                            sshClient.Disconnect();
                            break;
                        }
                    }
                    else
                        Task.Delay(500, cancelToken.Token).Wait(); // End of Output...Wait for New Input before Reading Again.
                }
                catch { break; } /// We must close the stream to cancel / end which may throw
            }
            return result.IsCompleted;
        }
    }

    public void Cancel()
    {
        cancelToken?.Cancel();
        asyncCommand?.OutputStream?.Close();
    }
}
