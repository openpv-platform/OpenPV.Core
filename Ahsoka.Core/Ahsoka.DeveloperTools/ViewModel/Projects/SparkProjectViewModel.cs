using Ahsoka.Core.Utility;
using Avalonia.Threading;
using EnovationCloud.Core.DataTransferObjects;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal partial class SparkProjectViewModel : ChildViewModelBase<SparkProjectEditorViewModel>
{
    TeamDTO model;

    public SparkProjectApplicationViewModel Applications { get; init; }

    public SparkProjectDevicesViewModel Devices { get; init; }

    public SparkProjectArtifactsViewModel Artifacts { get; init; }

    public SparkProjectMembersViewModel Members { get; init; }

    public SparkProjectModulesViewModel Modules { get; init; }

    public bool IsAdmin
    {
        get { return (IsOwner) || model.MemberRole == MemberRole.Administrator; }
    }

    public bool IsFavorite
    {
        get { return ParentViewModel.ParentViewModel.Settings.FavoriteTeams.Contains(this.TeamID); }
        set
        {
            if (value)
            {
                if (!ParentViewModel.ParentViewModel.Settings.FavoriteTeams.Contains(this.TeamID))
                    ParentViewModel.ParentViewModel.Settings.FavoriteTeams.Add(this.TeamID);
            }
            else
                ParentViewModel.ParentViewModel.Settings.FavoriteTeams.Remove(this.TeamID);

            OnPropertyChanged();
            ParentViewModel.ParentViewModel.SaveSettings();
            ParentViewModel.RefreshTeamList();
        }
    }

    public string TeamName
    {
        get { return model.TeamName; }
        set { model.TeamName = value; OnPropertyChanged(); }
    }

    public string TeamID
    {
        get { return model.TeamID.ToString(); }
    }

    public string OwnerEmailAddress
    {
        get { return (from x in Members.TeamMembers where x.IsOwner select x.MemberEmailAddress).FirstOrDefault(); }
        set { }
    }

    public bool IsOwner
    {
        get { return model.IsOwner; }
    }

    public bool IsViewer
    {
        get { return model.MemberRole is MemberRole.Viewer or MemberRole.Production; }
    }

    public string TeamIDShort
    {
        get { return model.TeamID.ToString()[..8]; }
    }

    public TeamDTO Model { get => model; set => model = value; }

    public override string ToString()
    {
        return $"{TeamName} ({TeamIDShort})";
    }

    public SparkProjectViewModel(SparkProjectEditorViewModel teamVM, TeamDTO dto) : base(teamVM)
    {
        this.model = dto;
        Applications = new(this);
        Devices = new(this);
        Artifacts = new(this);
        Members = new(this);
        Modules = new(this);
    }

    internal void LoadTeam()
    {
        ParentViewModel.WorkingText = "Refreshing Team...";
        ParentViewModel.IsWorking = true;

        _ = Task.Run(() =>
        {
            var model = new SparkProjectViewModel(ParentViewModel, this.model);
            model.Load();
            return model;

        }).ContinueWith(result =>
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    int index = ParentViewModel.Teams.IndexOf(this);
                    ParentViewModel.Teams.Remove(this);
                    ParentViewModel.Teams.Insert(index, result.Result);
                    ParentViewModel.SelectedTeam = result.Result;
                });
            }
            finally { ParentViewModel.IsWorking = false; }
        });
    }

    internal void Load()
    {
        Modules.GetTeamModules();
        Artifacts.LoadKeys();
    }

    internal void FavoriteChanged()
    {
        this.IsFavorite = !IsFavorite;
    }

}
