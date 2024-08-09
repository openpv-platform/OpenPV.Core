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
    public string LastResult { get; set; }

    [BindProperty]
    public UsbInfo UsbInfo { get; set; }

    public IndexModel()
    {
    }

    public void OnGet()
    {
        HardwareInfo = AppModel.GetHardwareInformation();
        UsbInfo = AppModel.GetUsbInfo();
        LastResult = AppModel.GetLastResult();
    }

    public PartialViewResult OnGetInfoPartial()
    {
        HardwareInfo = AppModel.GetHardwareInformation();
        return Partial("CpuPartial", this);
    }

    public PartialViewResult OnGetRolePartial()
    {
        AppModel.ToggleUsbInfo();
        UsbInfo = AppModel.GetUsbInfo();
        LastResult = AppModel.GetLastResult();
        return Partial("RolePartial", this);
    }
}
