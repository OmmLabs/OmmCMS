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
        var cmsPreview = bool.TryParse(Services.ApplicationContext.Configuration["OmmCMS:PreviewShell"], out var enabled) && enabled;
        var cacheKey = "{0}-{1}-{2}".FormatInvariant(
            Services.WorkContext.WorkingLanguage.Id,
            Services.WorkContext.CurrentCustomer.GetRolesIdent(),
            cmsPreview ? "cms" : "store");

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
                Id = "catalog",
                Text = T("Admin.Catalog"),
                Icon = "box",
                PermissionNames = perm,
                ControllerName = "Product",
                ActionName = "CatalogSettings"
            },
            new MenuItem
            {
                Id = "search",
                Text = T("Search.Title"),
                Icon = "search",
                PermissionNames = perm,
                ControllerName = "Search",
                ActionName = "SearchSettings"
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
                Id = "cart",
                Text = T("ShoppingCart"),
                Icon = "cart",
                PermissionNames = perm,
                ControllerName = "ShoppingCart",
                ActionName = "ShoppingCartSettings"
            },
            new MenuItem
            {
                Id = "order",
                Text = T("Admin.Orders"),
                Icon = "chart-up",
                PermissionNames = perm,
                ControllerName = "Order",
                ActionName = "OrderSettings"
            },
            new MenuItem
            {
                Id = "finance",
                Text = T("Common.Finance"),
                Icon = "discount",
                PermissionNames = perm,
                ControllerName = "Tax",
                ActionName = "FinanceSettings"
            },
            new MenuItem
            {
                Id = "shipping",
                Text = T("Admin.Configuration.Shipping"),
                Icon = "truck",
                PermissionNames = perm,
                ControllerName = "Shipping",
                ActionName = "ShippingSettings"
            },
            new MenuItem
            {
                Id = "reward-points",
                Text = T("Account.RewardPoints"),
                Icon = "trophy",
                PermissionNames = perm,
                ControllerName = "Customer",
                ActionName = "RewardPointsSettings"
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

        // Keep settings navigation in sync with the CMS profile.
        if (bool.TryParse(Services.ApplicationContext.Configuration["OmmCMS:PreviewShell"], out var cmsEnabled) && cmsEnabled)
        {
            var commerceSettingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "catalog", "search", "cart", "order", "payment", "finance", "shipping",
                "reward-points", "dataexchange", "all"
            };

            foreach (var child in root.Children)
            {
                if (commerceSettingIds.Contains(child.Value.Id))
                {
                    child.Value.Visible = false;
                }
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
