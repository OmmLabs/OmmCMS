using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components;

public class ShopBarViewComponent : SmartViewComponent
{
    private readonly CustomerSettings _customerSettings;

    public ShopBarViewComponent(CustomerSettings customerSettings)
    {
        _customerSettings = customerSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        var isAdmin = customer.IsAdmin();
        var isRegistered = isAdmin || customer.IsRegistered();

        var model = new ShopBarModel
        {
            IsAuthenticated = isRegistered,
            CustomerEmailUsername = isRegistered ? (_customerSettings.SupportsUsernames() ? customer.Username : customer.Email) : string.Empty,
            IsCustomerImpersonated = Services.WorkContext.CurrentImpersonator != null,
            DisplayAdminLink = await Services.Permissions.AuthorizeAsync(Permissions.System.AccessBackend),
            PublicStoreNavigationAllowed = await Services.Permissions.AuthorizeAsync(Permissions.System.AccessShop)
        };

        return View(model);
    }
}