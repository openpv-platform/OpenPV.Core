using Ahsoka.Core.Utility;
using Avalonia.Controls;
using System.Linq;

namespace Ahsoka.DeveloperTools.ViewModel;

internal class PopupViewModel : ViewModelBase
{
    private readonly MainViewModel parentViewModel;
    Grid popupControl;

    #region Popup Management
    public void ShowPopup(UserControl popupControl)
    {
        if (parentViewModel.Console.ConsoleVisible)
            parentViewModel.Console.HideConsole();

        if (parentViewModel.Updates.UpdatesVisible)
            parentViewModel.Updates.HideUpdates();

        PopupUserControl.Children.Add(popupControl);
    }

    public void DismissPopup(UserControl popupControl = null)
    {
        if (popupControl != null)
            PopupUserControl?.Children.Remove(popupControl);
        else
            foreach (var item in PopupUserControl.Children.ToArray())
                PopupUserControl?.Children.Remove(item);

    }

    public UserControl RootPopup
    {
        get
        {
            return PopupUserControl.Children.Count > 0 ? PopupUserControl.Children.First() as UserControl : new UserControl();
        }
    }

    public Grid PopupUserControl
    {
        get { return popupControl; }
        private set { popupControl = value; OnPropertyChanged(); }
    }

    public PopupViewModel(MainViewModel parentViewModel)
    {
        PopupUserControl = new Grid();
        this.parentViewModel = parentViewModel;
    }
    #endregion
}
