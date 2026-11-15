using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Security;
using OmmCMS.Web.Models.Common;
using OmmCMS.Web.Rendering;
using OmmCMS.Web.Rendering.Builders;

namespace OmmCMS.Web.Components;

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