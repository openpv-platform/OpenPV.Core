using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.Security;
using Ahsoka.Services.System;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Ahsoka.DeveloperTools.ViewModel;

internal class ActiveConnectionViewModel : ViewModelBase
{
    #region Connection Management Fields
    ConnectionInfo selectedInfo;
    bool isConnected = false;
    bool isInstaller = false;
    GetHardwareInformationReply hardwareInfo;
    string connectionString = string.Empty;
    private readonly MainViewModel parentViewModel;
    #endregion

    #region Properties
    public bool IsConnected
    {
        get { return isConnected; }
        set
        {
            isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectionColor));
        }
    }

    public bool IsInstaller
    {
        get { return isInstaller; }
        set
        {
            isInstaller = value;
            OnPropertyChanged();
        }
    }

    public GetHardwareInformationReply ConnectedHardwareInfo
    {
        get { return hardwareInfo; }
        set
        {
            hardwareInfo = value;
            OnPropertyChanged();
        }
    }

    public ConnectionInfo Connection
    {
        get { return selectedInfo; }
        set
        {
            if (selectedInfo != null)
                selectedInfo.IsActive = false;

            selectedInfo = value;
            if (selectedInfo != null)
                selectedInfo.IsActive = true;

            OnPropertyChanged();
        }
    }

    public string ConnectionDescription
    {
        get { return connectionString; }
        set { connectionString = value; OnPropertyChanged(); }
    }

    public ISolidColorBrush ConnectionColor
    {
        get { return IsConnected ? Brushes.SteelBlue : Brushes.Gray; }
        set { }
    }
    #endregion

    public ActiveConnectionViewModel(MainViewModel mainViewModel)
    {
        this.parentViewModel = mainViewModel;
    }

    public void ShowConnectionDetails()
    {
        if (parentViewModel.Popup.RootPopup is ConnectionDetailsView)
            parentViewModel.Popup.DismissPopup();
        else
            parentViewModel.Popup.ShowPopup(new ConnectionDetailsView() { DataContext = this });
    }

    public void ClosePopup()
    {
        parentViewModel.Popup.DismissPopup();
    }
}


