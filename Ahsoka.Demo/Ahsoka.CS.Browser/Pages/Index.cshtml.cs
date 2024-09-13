using Ahsoka.CS.Browser;
using Ahsoka.Services.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ahoska.CS.Browser.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public GetHardwareInformationReply HardwareInfo { get; set; }

    [BindProperty]
    public int Brightness { get; set; }

    public IndexModel()
    {
    }

    public void OnGet()
    {
        HardwareInfo = AppModel.GetHardwareInformation();
        Brightness = AppModel.GetBrightness();
    }

    public PartialViewResult OnGetInfoPartial()
    {
        HardwareInfo = AppModel.GetHardwareInformation();
        return Partial("CpuPartial", this);
    }

    public PartialViewResult OnGetBrightnessUpPartial()
    {
        AppModel.BrightnessUp();
        Brightness = AppModel.GetBrightness();
        return Partial("BrightnessPartial", this);
    }

    public PartialViewResult OnGetBrightnessDownPartial()
    {
        AppModel.BrightnessDown();
        Brightness = AppModel.GetBrightness();
        return Partial("BrightnessPartial", this);
    }
}
