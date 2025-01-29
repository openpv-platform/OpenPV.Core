using Avalonia.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools.Core;

public interface ICustomerToolViewModel
{
    Task<string> FolderLocatorAsync(string dialogTitle, string defaultPath = "");
    Task<string> FileLocatorAsync(string dialogTitle, string extension, string fileType, string currentFile = "");
    Task<bool> ShowDialog(string dialogTitle, string dialogText, string dialogAcceptText, string dialogRejectText = "");

    void StartProcess(bool clearConsole = true);
    void FinishProcess(string result, bool closeConsole);

    void AddConsoleLine(string consoleLine);
    void AddConsoleText(string consoleLine);

    void ShowPopup(UserControl popupControl);
    void DismissPopup(UserControl popupControl = null);

    void SdkNeedsUpdate();

    void RunCommandLineProcess(string processArgs, string workingDirectory, IProgress<CommandLineProgressInfo> progressString, CancellationToken token);
}
