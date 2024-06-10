using Ahsoka.CustomerTools.Views;
using Ahsoka.Spark;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

/// <summary>
/// Members
/// </summary>
internal partial class SparkProjectMembersViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    UserControl selectView;
    NewItemView addItemView;

    public ObservableCollection<TeamDTO> TeamMembers
    {
        get;
        init;

    } = new();

    public TeamDTO SelectedTeamMember
    {
        get;
        set;
    }

    public SparkProjectMembersViewModel(SparkProjectViewModel teamVM) : base(teamVM)
    {
    }

    internal void AddMemberAccepted()
    {
        var teamVM = FindParent<SparkProjectEditorViewModel>();
        teamVM.IsWorking = true;
        teamVM.WorkingText = "Adding Member...";

        string emailAddress = ((NewItemViewModel)addItemView.DataContext).ItemText;

        Task.Run(() =>
        {
            if (TeamMembers.Any(x => x.MemberEmailAddress.ToLowerInvariant() == emailAddress.ToLowerInvariant()))
                throw new ApplicationException("Member Already Exists");

            return SparkProjectTools.AddTeamMember(this.ParentViewModel.Model.TeamID, emailAddress) != null;

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                    {
                        if (result.Exception?.InnerException is ApplicationException)
                            teamVM.SetResultText(result.Exception.InnerException.Message, true);
                        else
                            teamVM.SetResultText("Error Adding Member, Check that Member is a User in Spark", true);

                        Cancel(); // Close Dialog
                        return;
                    }

                    if (result.Result)
                    {
                        LoadTeamMembers();

                        Cancel(); // Close Dialog
                        teamVM.SetResultText("Member Added", false);

                    }
                    else
                    {
                        teamVM.SetResultText("Member Add Failed or Member Email is Not Found", true);
                    }
                }
                finally
                {
                    teamVM.IsWorking = false;
                }

            });
        });
    }

    internal void ShowMembers()
    {
        selectView = new SparkProjectMemberListView() { DataContext = this };
        FindParent<MainViewModel>().Popup.ShowPopup(selectView);
    }

    internal void Cancel()
    {
        if (addItemView != null)
            FindParent<MainViewModel>().Popup.DismissPopup(addItemView);

        addItemView = null;
    }

    internal void Back()
    {
        if (selectView != null)
            FindParent<MainViewModel>().Popup.DismissPopup(selectView);

        selectView = null;
    }

    internal void AddMember()
    {
        var model = new NewItemViewModel()
        {
            Hint = "*Note: Team Members will also have access to create and manage apps and devices.",
            Label = "Member Email:",
            Title = "Add Team Member",
            ItemAccepted = AddMemberAccepted,
            ItemCancelled = Cancel
        };
        this.addItemView = new NewItemView() { DataContext = model };
        FindParent<MainViewModel>().Popup.ShowPopup(addItemView);
    }

    internal async void RemoveMember()
    {
        if (SelectedTeamMember != null)
        {
            var continueRemove = await FindParent<MainViewModel>().Dialog.ShowDialog("Remove?", "Are you sure you want remove the selected project member?", "Yes", "Cancel");
            if (!continueRemove)
                return;

            var teamVM = FindParent<SparkProjectEditorViewModel>();
            teamVM.IsWorking = true;
            teamVM.WorkingText = "Removing Member...";

            _ = Task.Run(() =>
            {
                if (this.SelectedTeamMember.IsOwner)
                    throw new ApplicationException("Can't Delete Owner, Delete Team Instead.");

                SparkProjectTools.RemoveTeamMember(this.ParentViewModel.Model.TeamID, SelectedTeamMember.MemberEmailAddress);

            }).ContinueWith(result =>
            {

                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                        {
                            teamVM.SetResultText(result.Exception.InnerException.Message, true);
                            return;
                        }

                        LoadTeamMembers();

                        teamVM.SetResultText("Member Removed", false);

                    });
                }
                finally { teamVM.IsWorking = false; }
            });
        }
    }

    internal void LoadTeamMembers()
    {
        this.TeamMembers.Clear();
        foreach (var item in SparkProjectTools.GetTeamMembers(this.ParentViewModel.Model.TeamID))
            this.TeamMembers.Add(item);

        this.SelectedTeamMember = this.TeamMembers.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedTeamMember));
    }


    #region RoleChanges
    public void ChangeRoleAdmin()
    {
        ChangeRole(MemberRole.Administrator);
    }

    public void ChangeRoleViewer()
    {
        ChangeRole(MemberRole.Viewer);
    }

    public void ChangeRoleMember()
    {
        ChangeRole(MemberRole.Member);
    }

    public void ChangeRoleProduction()
    {
        ChangeRole(MemberRole.Production);
    }

    private void ChangeRole(MemberRole newRole)
    {
        var memberList = TeamMembers.ToArray();
        var member = SelectedTeamMember;

        if (!ParentViewModel.IsOwner)
            return;

        var teamVM = FindParent<SparkProjectEditorViewModel>();

        teamVM.WorkingText = "Refreshing Members...";
        teamVM.IsWorking = true;

        _ = Task.Run(() =>
        {
            member.MemberRole = newRole;

            // Forcing a new object for now until web is connected
            member = JsonUtility.Deserialize<TeamDTO>(JsonUtility.Serialize(member));
            SparkTeamClient.SharedSparkClient.ChangeMemberRole(member.TeamID, member.MemberEmailAddress, member.MemberRole); 

        }).ContinueWith(result =>
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    int index = TeamMembers.IndexOf(SelectedTeamMember);
                    TeamMembers.Remove(SelectedTeamMember);
                    TeamMembers.Insert(index, member);
                    SelectedTeamMember = member;
                    OnPropertyChanged(nameof(SelectedTeamMember));
                });
            }
            finally
            {
                teamVM.IsWorking = false;
            }
        });
    }

    #endregion
}
