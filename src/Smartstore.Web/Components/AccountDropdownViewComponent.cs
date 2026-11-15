using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Web.Components;

public class AccountDropdownViewComponent : SmartViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customer = Services.WorkContext.CurrentCustomer;

        var model = new AccountDropdownModel
        {
            IsAuthenticated = customer.IsRegistered(),
            DisplayAdminLink = await Services.Permissions.AuthorizeAsync(Permissions.System.AccessBackend),
        };

        model.MenuItems.Add(new MenuItem().ToBuilder()
            .Action("Info", "Customer")
            .LinkHtmlAttributes(new { @class = "dropdown-item", rel = "nofollow" })
            .Icon("user")
            .IconClass("icon-fw")
            .Text(T("Account.MyAccount"))
            .AsItem());

        if (model.DisplayAdminLink)
        {
            model.MenuItems.Add(new MenuItem().ToBuilder()
                .Url("~/admin")
                .LinkHtmlAttributes(new { @class = "dropdown-item", rel = "nofollow", target = "_admin" })
                .Icon("gear")
                .IconClass("icon-fw")
                .Text(T("Account.Administration"))
                .AsItem());
        }

        model.MenuItems.Add(new MenuItem().ToBuilder().Id("logout-divider").IsGroupHeader(true).AsItem());

        model.MenuItems.Add(new MenuItem().ToBuilder()
            .Route("Logout")
            .LinkHtmlAttributes(new { @class = "dropdown-item", rel = "nofollow" })
            .Icon("logout")
            .IconClass("icon-fw")
            .Text(T("Account.Logout"))
            .AsItem());

        return View(model);
    }
}