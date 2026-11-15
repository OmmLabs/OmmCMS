using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Security;
using OmmCMS.Web.Models.Common;

namespace OmmCMS.Web.Components;

public class TopBarViewComponent : SmartViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        var isRegistered = customer.IsRegistered();

        var model = new TopBarModel
        {
            CustomerEmailUsername = isRegistered ? HttpContext.User.Identity.Name : string.Empty,
            IsCustomerImpersonated = Services.WorkContext.CurrentImpersonator != null,
            IsAuthenticated = isRegistered,
            DisplayAdminLink = Services.Permissions.Authorize(Permissions.System.AccessBackend),
            HasContactUsPage = (await Url.TopicAsync("ContactUs")).ToString().HasValue()
        };

        return View(model);
    }
}