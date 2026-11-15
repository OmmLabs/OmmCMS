using OmmCMS.Core.Identity;
using OmmCMS.Core.Packaging;
using OmmCMS.Core.Security;
using OmmCMS.Core.Stores;

namespace OmmCMS.Admin.Components;

public class AdminNavbarViewComponent : SmartViewComponent
{
    private readonly CustomerSettings _customerSettings;
    private readonly Lazy<UpdateChecker> _updateChecker;

    public AdminNavbarViewComponent(CustomerSettings customerSettings, Lazy<UpdateChecker> updateChecker)
    {
        _customerSettings = customerSettings;
        _updateChecker = updateChecker;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentCustomer = Services.WorkContext.CurrentCustomer;

        ViewBag.UserName = _customerSettings.SupportsUsernames() ? currentCustomer.Username : currentCustomer.Email;
        ViewBag.Stores = Services.StoreContext.GetAllStores();
        if (await Services.Permissions.AuthorizeAsync(Permissions.System.Maintenance.Read))
        {
            ViewBag.CheckUpdateResult = await _updateChecker.Value.CheckUpdateAsync(false);
        }

        return View();
    }
}