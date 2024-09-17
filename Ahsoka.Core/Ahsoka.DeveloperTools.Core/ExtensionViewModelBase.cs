using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using System;

namespace Ahsoka.DeveloperTools.Core;

public abstract class ExtensionViewModelBase : ViewModelBase
{
    protected ICustomerToolViewModel CustomerToolViewModel { get; set; }

    public bool InitComplete { get; private set; }

    public void InitExtension(ICustomerToolViewModel vmParent, HardwareInfo hardwareInfo, string projectFolder, string configFile)
    {
        CustomerToolViewModel = vmParent;

        OnInitExtension(hardwareInfo, projectFolder, configFile);

        InitComplete = true;
    }

    public UserControl GetView()
    {
        return OnGetView();
    }

    public bool HasChanges()
    {
        return OnHasChanges();
    }

    public void Close()
    {
        OnClose();
    }

    public string Save(string packageInfoFolder)
    {
        return OnSave(packageInfoFolder);
    }

    public void CloseUI()
    {
        OnCloseUI();
    }

    protected abstract void OnInitExtension(HardwareInfo hardwareInfo, string projectFolder, string configurationPath);

    protected abstract string OnSave(string packageInfoFolder);

    protected abstract UserControl OnGetView();

    protected abstract bool OnHasChanges();

    protected abstract void OnClose();

    protected virtual void OnCloseUI()
    {

    }
}
