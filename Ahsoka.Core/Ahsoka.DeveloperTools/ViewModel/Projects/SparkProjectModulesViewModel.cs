using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Views;
using Avalonia.Controls;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;


/// <summary>
/// Modules
/// </summary>
internal partial class SparkProjectModulesViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    UserControl selectView;
    UserControl addproductView;
    PurchaseOrderDTOCollection purchaseInfo;
    string cartInfo;

    internal bool HasModules
    {
        get { return ModuleList.Count > 0; }
        set { }
    }

    internal ObservableCollection<LicensedModuleViewModel> ModuleList
    {
        get;
        private set;
    } = new ObservableCollection<LicensedModuleViewModel>();

    internal ObservableCollection<ModuleViewModel> AvailableModules
    {
        get;
        private set;
    } = new ObservableCollection<ModuleViewModel>();

    public string CartInfo
    {
        get { return cartInfo; }
        set { cartInfo = value; OnPropertyChanged(); }
    }

    public SparkProjectModulesViewModel(SparkProjectViewModel teamVM) : base(teamVM)
    {

    }

    internal void ShowModules()
    {
        selectView = new SparkProjectModuleList() { DataContext = this };
        FindParent<MainViewModel>().Popup.ShowPopup(selectView);
    }

    internal void GetTeamModules()
    {
        var teamModules = SparkAccountClient.SharedSparkClient.GetTeamModules(ParentViewModel.TeamID);

        this.ModuleList.Clear();
        foreach (var module in teamModules)
        {
            this.ModuleList.Add(new LicensedModuleViewModel(this)
            {
                ModuleID = module.Module.ID,
                ModuleName = module.Module.Name,
                ModuleDescription = module.Module.Description,
                Packages = CreatePackageViewModels(module.Module.Packages),
                TotalQuantity = module.QuantityPurchased,
                TotalUsed = module.QuantityUsed
            });
        }

        OnPropertyChanged(nameof(HasModules));

        ParentViewModel.ParentViewModel.SaveOfflineTeams();
    }

    public void PurchaseAdditionalLicenses(LicensedModuleViewModel vm)
    {
        this.AvailableModules.Clear();

        this.AvailableModules.Add(new ModuleViewModel(this)
        {
            ModuleName = vm.ModuleName,
            ModuleID = vm.ModuleID,
            ModuleDescription = vm.ModuleDescription,
            Packages = vm.Packages,
            IsSelected = true
        });

        this.addproductView = new SparkProjectUpdateModuleList() { DataContext = this };
        UpdateCart();
        FindParent<MainViewModel>().Popup.ShowPopup(addproductView);
    }

    internal void AddModule()
    {
        this.AvailableModules.Clear();

        var modules = SparkAccountClient.SharedSparkClient.GetModules();

        foreach (var module in modules)
        {
            if (!ModuleList.Any(x => x.ModuleID == module.ID))
                this.AvailableModules.Add(new ModuleViewModel(this)
                {
                    ModuleName = module.Name,
                    ModuleID = module.ID,
                    ModuleDescription = module.Description,
                    Packages = CreatePackageViewModels(module.Packages),
                });
        }

        this.addproductView = new SparkProjectAddModuleList() { DataContext = this };
        FindParent<MainViewModel>().Popup.ShowPopup(addproductView);
    }

    internal List<ModulePackageViewModel> CreatePackageViewModels(List<ProductPackageDTO> packages)
    {
        List<ModulePackageViewModel> result = new();
        foreach (var package in packages)
        {
            result.Add(new ModulePackageViewModel()
            {
                Package = package,
            });
        }
        return result;
    }


    internal void Back()
    {
        if (selectView != null)
        {
            FindParent<MainViewModel>().Popup.DismissPopup(selectView);
            selectView = null;
        }
    }

    internal async Task PurchaseNewModules()
    {
        var root = FindParent<MainViewModel>();
        root.Popup.DismissPopup(addproductView);

        if (!ParentViewModel.IsAdmin)
        {
            await root.Dialog.ShowDialog("Permission Failed",
                    "Only Admins can purchase modules", "Ok");

            return;
        }

        var continueWork = await root.Dialog.ShowDialog("Purchase Modules",
                "Are you sure you wish to purchase modules?  This will launch your browser to complete your transaction.", "Yes", "No");

        if (continueWork)
        {
            string url = "";
            try
            {
                url = SparkAccountClient.SharedSparkClient.CreateModuleCheckout(ParentViewModel.TeamID, purchaseInfo);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true,
                });
            }
            catch
            {
                _ = root.Dialog.ShowDialog("Error Detected",
                   url, "Ok");
                return;
            }

            await Task.Run(async () =>
            {
                var checkoutCanceled = root.Dialog.ShowDialog("Checkout",
                        "Please finalize purchase in your browser or press cancel to invalidate", "Cancel");

                var count = ParentViewModel.Modules.ModuleList.Count;
                do
                {
                    GetTeamModules();

                    if (checkoutCanceled.IsCompleted)
                    {
                        SparkAccountClient.SharedSparkClient.InvalidateCheckout(ParentViewModel.TeamID);
                        return false;
                    }

                    await Task.Delay(5000);
                } while (ParentViewModel.Modules.ModuleList.Count == count);
                return true;
            }).ContinueWith(result =>
            {
                if (result.Result)
                {
                    root.Dialog.CancelMessage();
                }
            });
        }
    }

    internal async Task PurchaseModules()
    {
        var root = FindParent<MainViewModel>();
        root.Popup.DismissPopup(addproductView);

        if (!ParentViewModel.IsAdmin)
        {
            await root.Dialog.ShowDialog("Permission Failed",
                    "Only Admins can purchase modules", "Ok");

            return;
        }

        var continueWork = await root.Dialog.ShowDialog("Adding Additional Licenses",
                "Are you sure you wish to add licenses?  This will attempt a transaction with your default payment method. May take a moment for results to become visible", "Yes", "No");

        if (continueWork)
        {
            var result = SparkAccountClient.SharedSparkClient.AddTeamModules(ParentViewModel.TeamID, purchaseInfo);
            if (result.ToLower().Contains("error") || result.ToLower().Contains("false"))
            {
                _ = root.Dialog.ShowDialog("Purchase Error",
                    "Payment could not be processed at this time. Please try again later or confirm payment method in billing portal.", "Ok");

                this.ModuleList.FirstOrDefault()?.ViewHistory();
                return;
            }

            GetTeamModules();

            _ = root.Dialog.ShowDialog("Payment Success",
                    "Process completed. Invoice is available through the billing portal", "Ok");
        }
    }

    internal void UpdateCart()
    {
        var stringBuilder = new StringBuilder();
        purchaseInfo = new();
        foreach (var item in AvailableModules)
        {
            if (item.IsSelected)
            {
                stringBuilder.AppendLine($"{item.ModuleName} {item.TotalQuantity}");
                purchaseInfo.Add(new PurchaseOrderDTO() { ProductID = item.ModuleID, PriceID = item.SelectedPackage.Package.PriceID, Quantity = item.PurchaseQuantity });
            }
        }

        if (stringBuilder.Length == 0)
            stringBuilder.AppendLine("No Products Selected");

        CartInfo = stringBuilder.ToString();
    }

    internal void CancelPurchase()
    {
        FindParent<MainViewModel>().Popup.DismissPopup(addproductView);
    }

}
