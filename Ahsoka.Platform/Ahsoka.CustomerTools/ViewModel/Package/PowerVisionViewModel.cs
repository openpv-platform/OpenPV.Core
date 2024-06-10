using Ahsoka.Installer;
using Ahsoka.Utility;

namespace Ahsoka.CustomerTools;

internal class PowerVisionViewModel : ChildViewModelBase<PackageViewModel>
{
    public PowerVisionViewModel(PackageViewModel parentViewModel)
        :base(parentViewModel)
    {
    }
}