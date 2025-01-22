using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class InstallerFileList
{
    Task refreshTask = null;
    CancellationTokenSource tokenSource = null;
    readonly List<string> fileList = new();
    int selectedFile;
    readonly string path;

    public event EventHandler<EventArgs> FileChanged;

    public bool Listen
    {
        get
        {
            return refreshTask != null;
        }
        set
        {
            if (value && refreshTask == null)
            {
                tokenSource = new CancellationTokenSource();
                refreshTask = Task.Run(() =>
                {
                    while (!tokenSource.IsCancellationRequested)
                    {
                        Refresh();
                        Task.Delay(1000).Wait();
                    }
                }, tokenSource.Token);
            }
            else
            {
                tokenSource.Cancel();
                refreshTask = null;
            }
        }
    }

    public string SelectedFile
    {
        get
        {
            return fileList.Count > selectedFile ? fileList[selectedFile] : string.Empty;
        }
    }

    public bool HasInstallPackage
    {
        get
        {
            return fileList.Count > 0;
        }
    }

    public void MoveUp()
    {
        selectedFile--;

        if (selectedFile >= fileList.Count)
            selectedFile = 0;
        else if (selectedFile < 0)
            selectedFile = fileList.Count - 1;
    }

    public void MoveDown()
    {
        selectedFile++;

        if (selectedFile >= fileList.Count)
            selectedFile = 0;
        else if (selectedFile < 0)
            selectedFile = fileList.Count - 1;
    }

    public InstallerFileList()
    {
        path = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private void Refresh()
    {
        string currentSelectedItem = SelectedFile;

        var tempList = new List<string>(fileList.ToArray());

        fileList.Clear();

        selectedFile = tempList.IndexOf(currentSelectedItem);

        if (selectedFile < 0)
            selectedFile = 0;

        if (Directory.Exists(path))
        {
            foreach (var item in Directory.GetFiles(path))
                if (item.EndsWith(PackageType.opvEncrypted.GetFileExtension()))
                    fileList?.Add(item);
        }

        if (!tempList.SequenceEqual(fileList))
        {
            FileChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
