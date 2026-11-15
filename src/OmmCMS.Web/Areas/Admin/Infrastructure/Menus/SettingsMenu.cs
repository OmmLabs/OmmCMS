using OmmCMS.Caching;
using OmmCMS.Collections;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Security;

namespace OmmCMS.Admin.Infrastructure.Menus;

public partial class SettingsMenu : MenuBase
{
    public override string Name => "Settings";

    protected override string GetCacheKey()
    {
        var cacheKey = "{0}-{1}-cms".FormatInvariant(
            Services.WorkContext.WorkingLanguage.Id,
            Services.WorkContext.CurrentCustomer.GetRolesIdent());

        return cacheKey;
    }

    protected override Task<TreeNode<MenuItem>> BuildAsync(CacheEntryOptions cacheEntryOptions)
    {
        var store = Services.StoreContext.CurrentStore;
        var customer = Services.WorkContext.CurrentCustomer;
        var perm = Permissions.Configuration.Setting.Read;

        var root = new TreeNode<MenuItem>(new MenuItem { Text = T("Admin.Configuration.Settings") })
        {
            Id = Name
        };

        root.AppendRange(
        [
            new MenuItem
            {
                Id = "general",
                Text = T("Admin.Common.General"),
                Icon = "sliders",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "GeneralCommon"
            },
            new MenuItem
            {
                Id = "customer",
                Text = T("Admin.Customers"),
                Icon = "user",
                PermissionNames = perm,
                ControllerName = "Customer",
                ActionName = "CustomerUserSettings"
            },
            new MenuItem
            {
                Id = "icons",
                Text = T("Admin.Configuration.Settings.Icons"),
                Icon = "star",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "IconSettings"
            },
            new MenuItem
            {
                Id = "media",
                Text = T("Admin.Plugins.KnownGroup.Media"),
                Icon = "images",
                PermissionNames = perm,
                ControllerName = "Media",
                ActionName = "MediaSettings"
            },
            new MenuItem
            {
                Id = "dataexchange",
                Text = T("Admin.Common.DataExchange"),
                Icon = "exchange",
                PermissionNames = perm,
                ControllerName = "Import",
                ActionName = "DataExchangeSettings"
            },
            new MenuItem
            {
                Id = "performance",
                Text = T("Admin.Configuration.Settings.Performance"),
                Icon = "bi:speedometer2!",
                PermissionNames = perm,
                ControllerName = "Maintenance",
                ActionName = "PerformanceSettings"
            },
            new MenuItem
            {
                IsGroupHeader = true,
                Id = "all",
                Text = T("Admin.Configuration.Settings.AllSettings"),
                Icon = "gear",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "AllSettings"
            }
        ]);

        // Keep unsupported settings navigation inaccessible in the CMS profile.
        foreach (var child in root.Children)
        {
            if (child.Value.Id is "dataexchange" or "all")
            {
                child.Value.Visible = false;
            }
        }

        // Add area = "Admin" to all items in one go.
        foreach (var item in root.Children)
        {
            item.Value.RouteValues["area"] = "Admin";
        }

        return Task.FromResult(root);
    }
}
