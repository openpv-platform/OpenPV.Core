using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.Utility;

namespace Ahsoka.CustomerTools;

internal class DeviceViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    private DeviceDTO model;

    public DeviceDTO Model { get => model; set => model = value; }

    public DeviceViewModel(SparkProjectViewModel teamVM, DeviceDTO dto) : base(teamVM)
    {
        this.Model = dto;
    }

    public string Description
    {
        get { return this.Model.Description; }
    }

    public string Track
    {
        get
        {
            if (this.Model.GroopID == ParentViewModel.Model.BetaDeviceGroup)
                return "Beta";
            else if (this.Model.GroopID == ParentViewModel.Model.ReleaseDeviceGroup)
                return "Release";

            return "Development";

        }
    }

    public string PartNumber
    {
        get { return this.Model.PartNumber; }
    }

    public string SerialNumber
    {
        get { return this.Model.SerialNumber; }
    }

    public string DeviceID
    {
        get { return this.Model.DeviceID.ToString(); }
    }
}
