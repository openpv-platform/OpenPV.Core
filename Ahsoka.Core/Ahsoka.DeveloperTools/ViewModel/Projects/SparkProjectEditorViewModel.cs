using Ahsoka.DeveloperTools.Views;
using Ahsoka.Spark;
using Ahsoka.Utility;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class SparkProjectEditorViewModel : ChildViewModelBase<MainViewModel>
{
    NewItemView addItemView;
    string resultText;
    bool isError = false;
    SparkProjectViewModel selectedTeam;
    bool hasAdminSubscriptions = false;
    readonly List<SubscriptionTierDTO> tiers = new();
    bool isWorking = false;
    string workingText;

    public bool IsWorking
    {
        get { return isWorking; }
        set { isWorking = value; OnPropertyChanged(); }
    }

    public string WorkingText
    {
        get { return workingText; }
        set { workingText = value; OnPropertyChanged(); }
    }

    public bool HasAdminSubscriptions { get => hasAdminSubscriptions; set => hasAdminSubscriptions = value; }

    public string ResultText
    {
        get { return resultText; }
        set { OnSetObject(ref resultText, value); }
    }

    public bool IsError
    {
        get { return isError; }
        set { OnSetValue(ref isError, value); }

    }

    public SubscriptionTierDTO[] SubscriptionTiers
    {
        get
        {
            return tiers.ToArray();
        }
    }

    public ObservableCollection<SparkProjectViewModel> Teams
    {
        get;
        private set;
    } = new();

    public SparkProjectViewModel SelectedTeam
    {
        get { return selectedTeam; }
        set
        {
            if (value?.Members.TeamMembers.Count == 0)
                value?.Members.LoadTeamMembers();

            if (value?.Artifacts.DevelopmentKeys.Count == 0)
                value?.Artifacts.LoadKeys();

            OnSetObject(ref selectedTeam, value);
        }
    }

    public SparkProjectEditorViewModel(MainViewModel ParentViewModel) :
        base(ParentViewModel)
    {
    }

    internal void CreateTeam()
    {
        var model = new NewItemViewModel()
        {
            Hint = "*Note: Project Names can't be changed after they are created",
            Label = "Project Name:",
            Title = "Create New Project",
            ItemAccepted = CreateTeamAccepted,
            ItemCancelled = Cancel
        };
        this.addItemView = new NewItemView() { DataContext = model };

        ParentViewModel.Popup.ShowPopup(addItemView);

    }

    internal void CreateTeamAccepted()
    {
        string name = ((NewItemViewModel)addItemView.DataContext).ItemText;

        if (String.IsNullOrEmpty(name))
        {
            ParentViewModel.Dialog.ShowDialog("New Project", "Project Name is Empty.", "Ok");
            return;
        }

        IsWorking = true;
        WorkingText = "Creating Team";

        ParentViewModel.Popup.DismissPopup(addItemView);

        Task.Run(() =>
        {
            return SparkProjectTools.AddTeam(new TeamDTO() { TeamName = name });

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                {
                    SetResultText("System was not able to add the new project.", true);
                    IsWorking = false;
                    return;
                }

                if (result.Result != null)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Cancel(); // Close Dialog
                    });

                    // Reload Teams
                    var newTeam = new SparkProjectViewModel(this, result.Result);
                    newTeam.Load();
                    this.Teams.Add(newTeam);
                    this.SelectedTeam = newTeam;

                    ParentViewModel.Dialog.ShowDialog("New Project", "Your new project is ready for use.  If your project requires module licenses you may select them now.  \r\n\r\nClick the 'Module Setup' tool on the bottom left to add module package to your project.", "Ok");
                    SetResultText("New Project Added", false);
                }
                else
                {
                    SetResultText("Could not Add Project", false);
                }


                IsWorking = false;

            });
        });
    }

    internal void Cancel()
    {
        if (addItemView != null)
            ParentViewModel.Popup.DismissPopup(addItemView);

        addItemView = null;
    }

    internal void ClosePopup()
    {
        ParentViewModel.Popup.DismissPopup();
    }

    internal void RenameTeam()
    {
        if (SelectedTeam != null)
        {
            if (SelectedTeam.IsAdmin)
            {
                var model = CreateRenameView("Are you Sure? Enter the Current Project Name to Verify.", EnterRename);
                this.addItemView = new NewItemView() { DataContext = model };

                ParentViewModel.Popup.ShowPopup(addItemView);
            }
            else
            {
                ParentViewModel.Dialog.ShowDialog("Rename Error", "You must be the project admin to rename this project", "Ok");
            }

        }
    }

    private void EnterRename()
    {
        string renameName = ((NewItemViewModel)addItemView.DataContext).ItemText;
        if (SelectedTeam.TeamName != renameName)
        {
            ParentViewModel.Dialog.ShowDialog("Rename Error", "Project name did not match", "Ok");
            return;
        }

        ParentViewModel.Popup.DismissPopup(addItemView);

        var model = CreateRenameView("Enter a new name for the project.", ExecuteRename);
        this.addItemView = new NewItemView() { DataContext = model };

        ParentViewModel.Popup.ShowPopup(addItemView);
    }

    private NewItemViewModel CreateRenameView(string title, Action accepted)
    {
        return new NewItemViewModel()
        {
            Label = "Project Name",
            Title = title,
            Hint = "This will rename the project, groups and all devices",
            ItemText = "RENAME",
            ItemAccepted = accepted,
            ItemCancelled = Cancel
        };
    }

    private void ExecuteRename()
    {
        string renameName = ((NewItemViewModel)addItemView.DataContext).ItemText;

        ParentViewModel.Popup.DismissPopup(addItemView);

        Task.Run(() =>
        {
            SparkAccountClient.SharedSparkClient.RenameTeam(SelectedTeam.TeamID, renameName);

        }).ContinueWith(result =>
        {
            if (result.Status != TaskStatus.RanToCompletion)
                SetResultText("Attempt to rename project was not successful.", true);
            else
            {
                SetResultText("Project Renamed.", false);
                LoadTeams().Wait();
            }
                
            IsWorking = false;           
        });
    }

    internal void RemoveTeam()
    {
        if (SelectedTeam != null)
        {
            if (SelectedTeam.IsOwner)
            {
                var model = new NewItemViewModel()
                {
                    Label = "Project Name",
                    Title = "Are you Sure? Enter the Project Name to Verify.",
                    Hint = "This will delete the project, groups and all devices",
                    ItemText = "DELETE",
                    ItemAccepted = RemoveTeamInternal,
                    ItemCancelled = Cancel
                };
                this.addItemView = new NewItemView() { DataContext = model };

                ParentViewModel.Popup.ShowPopup(addItemView);
            }
            else
            {
                ParentViewModel.Dialog.ShowDialog("Remove Error", "You must be the project owner to delete this project", "Ok");
            }

        }
    }

    internal void RemoveTeamInternal()
    {
        if (SelectedTeam != null)
        {
            string deleteName = ((NewItemViewModel)addItemView.DataContext).ItemText;
            if (!SelectedTeam.IsOwner || SelectedTeam.TeamName != deleteName)
            {
                ParentViewModel.Dialog.ShowDialog("Remove Error", "Project name did not match, the project was not deleted", "Ok");
                return;
            }

            SelectedTeam.Devices.LoadDevices();
            if (SelectedTeam.Devices.Devices.Count > 0)
            {
                ParentViewModel.Dialog.ShowDialog("Remove Error", "Projects with devices can not be deleted. Remove any devices before deleteing.", "Ok");
                return;
            }

            if (SelectedTeam.Modules.HasModules)
            {
                ParentViewModel.Dialog.ShowDialog("Remove Error", "Projects with existing purchases can not be removed.", "Ok");
                return;
            }



            IsWorking = true;
            WorkingText = "Removing Team";

            ParentViewModel.Popup.DismissPopup(addItemView);

            Task.Run(() =>
            {
                SparkProjectTools.RemoveTeam(SelectedTeam.Model);

            }).ContinueWith(result =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                    SetResultText("Attempt to remove project was not successful.", true);
                else
                    Teams.Remove(SelectedTeam);

                SetResultText("Project Removed.", false);

                IsWorking = false;
            });
        }
    }

    internal Task LoadTeams()
    {
        if (!ParentViewModel.LoggedIn)
            return null;

        if (ParentViewModel.IsOffline)
        {
            LoadOfflineTeams();
            return Task.CompletedTask;
        }

        // Set Team Sub
        HasAdminSubscriptions = SparkAccountClient.SharedSparkClient.IsAuthorizedForFeature("TeamSubscriptions");

        IsWorking = true;
        WorkingText = "Loading Projects";

        // Add tier to List
        var cachedTiers = SparkAccountClient.SharedSparkClient.GetSubscriptionTiers();
        tiers.Clear();
        foreach (var tier in cachedTiers)
            tiers.Add(tier);

        return Task.Run(() =>
        {
            List<SparkProjectViewModel> models = new();
            foreach (var item in SparkProjectTools.GetTeams())
            {
                var model = new SparkProjectViewModel(this, item);

                model.Load();

                models.Add(model);

            }
            return models;

        }).ContinueWith(result =>
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Teams.Clear();

                    // Add Team to List
                    foreach (var item in result.Result.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.TeamName))
                        Teams.Add(item);

                    SetResultText("Project Refreshed", false);

                    SaveOfflineTeams();
                });
            }
            finally { IsWorking = false; }
        });
    }

    internal void SaveOfflineTeams()
    {
         List<OffLineData> data = new();
        foreach(var item in Teams)
        {
            OffLineData dataItem = new();
            List<OfflineModule> collection = new();
            foreach (var module in item.Modules.ModuleList)
                collection.Add(new OfflineModule() { ModuleId = module.ModuleID, ModuleName = module.ModuleName });

            dataItem.Team = item.Model;
            dataItem.Modules = collection;
            data.Add(dataItem);
        }

        ParentViewModel.Settings.OfflineData = data;
        ParentViewModel.SaveSettings();
    }

    internal void LoadOfflineTeams()
    {
        Teams.Clear();

        foreach(var item in ParentViewModel.Settings.OfflineData)
        {
            var teamVM = new SparkProjectViewModel(this, item.Team);
            foreach(var module in item.Modules)
            {
                var moduleVM = new SparkProjectModulesViewModel(teamVM);
                teamVM.Modules.ModuleList.Add(new LicensedModuleViewModel(moduleVM) { ModuleID = module.ModuleId, ModuleName = module.ModuleName });
            }
            Teams.Add(teamVM);
          
        }
    }

    public void SetResultText(string text, bool isError)
    {
        IsError = isError;
        ResultText = text;
    }

    public void UpdateProperties()
    {
        OnPropertyChanged(nameof(SelectedTeam));

    }

    public void RefreshTeamList()
    {
        var item = this.SelectedTeam;
        Teams = new ObservableCollection<SparkProjectViewModel>(Teams.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.TeamName));
        OnPropertyChanged(nameof(Teams));
        this.selectedTeam = item;
    }
}

