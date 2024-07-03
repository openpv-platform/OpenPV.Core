using Ahsoka.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools.ViewModel;

internal class DialogViewModel : ViewModelBase
{
    // Dialog
    private readonly MainViewModel parentViewModel;
    string dialogText;
    string dialogTitle;
    string dialogAcceptText;
    string dialogRejectText;
    bool dialogVisible = false;
    bool showReject;
    AutoResetEvent autoResetEvent = new(false);
    bool dialogResult = true;

    public string DialogText
    {
        get { return dialogText; }
        set { dialogText = value; OnPropertyChanged(); }
    }


    public string DialogTitle
    {
        get { return dialogTitle; }
        set { dialogTitle = value; OnPropertyChanged(); }
    }

    public string DialogAcceptText
    {
        get { return dialogAcceptText; }
        set { dialogAcceptText = value; OnPropertyChanged(); }
    }

    public string DialogRejectText
    {
        get { return dialogRejectText; }
        set { dialogRejectText = value; OnPropertyChanged(); }
    }

    public bool DialogVisible
    {
        get { return dialogVisible; }
        set { dialogVisible = value; OnPropertyChanged(); }
    }

    public bool ShowReject
    {
        get { return showReject; }
        set { showReject = value; OnPropertyChanged(); }
    }

    #region Dialog Management
    public DialogViewModel(MainViewModel parentViewModel)
    {
        this.parentViewModel = parentViewModel;
    }

    internal Task<bool> ShowDialog(string dialogTitle, string dialogText, string dialogAcceptText, string dialogRejectText)
    {
        autoResetEvent = new(false);
        DialogVisible = true;
        this.DialogTitle = dialogTitle;
        this.DialogText = dialogText;
        this.DialogAcceptText = dialogAcceptText;
        this.DialogRejectText = dialogRejectText;
        this.ShowReject = !String.IsNullOrEmpty(dialogRejectText);
        return Task.Run(() =>
        {
            autoResetEvent.WaitOne();
            return dialogResult;
        });
    }

    internal Task<bool> ShowDialog(string dialogTitle, string dialogText, string dialogAcceptText)
    {
        return ShowDialog(dialogTitle, dialogText, dialogAcceptText, "");
    }

    internal void AcceptMessage()
    {
        DialogVisible = false;
        dialogResult = true;
        autoResetEvent.Set();
    }

    internal void CancelMessage()
    {
        DialogVisible = false;
        dialogResult = false;
        autoResetEvent?.Set();
    }
    #endregion

}
