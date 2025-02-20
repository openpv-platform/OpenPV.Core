using Ahsoka.Core.Utility;
using Avalonia.Media;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ahsoka.DeveloperTools;
internal class LicensedModuleViewModel : ChildViewModelBase<SparkProjectModulesViewModel>
{
    int totalQuantity;

    public IBrush BarColor
    {
        get { return PercentUsed > .8 ? Brushes.Salmon : Brushes.SteelBlue; }
        set { }
    }


    public string ModuleText
    {
        get { return $"Licenses: {TotalQuantity - TotalUsed} of {TotalQuantity} Remaining"; }
        set { }
    }

    public float PercentUsed
    {
        get { return (TotalUsed / (float)TotalQuantity); }
        set { }
    }

    public string ModuleID { get; set; }
    public string ModuleName { get; set; }
    public List<ModulePackageViewModel> Packages { get; set; }
    public string ModuleDescription { get; set; }
    public int TotalQuantity
    {
        get { return totalQuantity; }
        set
        {
            totalQuantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModuleText));
            OnPropertyChanged(nameof(PercentUsed));
            OnPropertyChanged(nameof(BarColor));
        }
    }

    public int TotalUsed { get; set; }

    public LicensedModuleViewModel(SparkProjectModulesViewModel teamVM) : base(teamVM)
    {

    }

    public async void ViewHistory()
    {
        if (!ParentViewModel.ParentViewModel.IsAdmin)
        {
            await ParentViewModel.ParentViewModel.ParentViewModel.ParentViewModel.Dialog.ShowDialog("Permission Failed",
                    "Only Admins can view purchase information", "Ok");

            return;
        }

        var continueWork = await ParentViewModel.ParentViewModel.ParentViewModel.ParentViewModel.Dialog.ShowDialog("View Payment Details?",
            "Viewing details will launch a window in your default browser. Continue?", "Yes", "No");

        if (continueWork)
        {
            try
            {
                var url = SparkAccountClient.SharedSparkClient.CreateBillingPortal(ParentViewModel.ParentViewModel.TeamID);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true,
                });
            }
            catch { }
        }
    }

    public void PurchaseAdditionalLicenses()
    {
        ParentViewModel.PurchaseAdditionalLicenses(this);
    }

}

internal class ModuleViewModel : ChildViewModelBase<SparkProjectModulesViewModel>
{
    bool isSelected = false;
    int quantity = 1;
    ModulePackageViewModel package = null;

    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            isSelected = value;
            OnPropertyChanged();
            ParentViewModel.UpdateCart();
        }
    }

    public ModulePackageViewModel SelectedPackage
    {
        get
        {
            return package ?? Packages.First();
        }
        set
        {
            package = value;
            OnPropertyChanged();
            OnPropertyChanged("TotalQuantity");
            ParentViewModel.UpdateCart();

        }
    }

    public int PurchaseQuantity
    {
        get
        {
            return quantity > 0 ? quantity : 1;
        }
        set
        {
            quantity = value;
            OnPropertyChanged();
            OnPropertyChanged("TotalQuantity");
            ParentViewModel.UpdateCart();
        }
    }

    public string ModuleID { get; set; }
    public string ModuleName { get; set; }
    public string ModuleDescription { get; set; }
    public List<ModulePackageViewModel> Packages { get; set; }
    public int TotalQuantity { get { return int.Parse(SelectedPackage.Package.NumberUnits) * PurchaseQuantity; } }

    public ModuleViewModel(SparkProjectModulesViewModel teamVM) : base(teamVM)
    {

    }

}

internal class ProvisionPackageViewModel
{
    public LicensedModuleViewModel Module { get; set; }

    public bool IsSelected { get; set; }

    public ProvisionPackageViewModel(LicensedModuleViewModel module)
    {
        this.Module = module;
        IsSelected = false;
    }
}

internal class ModulePackageViewModel
{
    public ProductPackageDTO Package { get; set; }

    public ModulePackageViewModel()
    {

    }

    public override string ToString()
    {
        return $"{Package.NumberUnits} units for ${Package.Price}";
    }
}

