using OmmCMS.Caching;
using OmmCMS.Collections;
using OmmCMS.Core.Data;
using OmmCMS.Diagnostics;

namespace OmmCMS.Core.Content.Menus;

/// <summary>
/// A generic implementation of <see cref="IMenu" /> which represents a <see cref="MenuEntity"/> entity.
/// </summary>
internal class DatabaseMenu : MenuBase
{
    private readonly IDictionary<string, Lazy<IMenuItemProvider, MenuItemProviderMetadata>> _menuItemProviders;

    public DatabaseMenu(
        string menuName,
        IEnumerable<Lazy<IMenuItemProvider, MenuItemProviderMetadata>> menuItemProviders)
    {
        Guard.NotEmpty(menuName);

        Name = menuName;

        _menuItemProviders = menuItemProviders.ToDictionarySafe(x => x.Metadata.ProviderName, x => x);
    }

    public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public override string Name { get; }

    public override bool ApplyPermissions => true;

    protected override async Task<TreeNode<MenuItem>> BuildAsync(CacheEntryOptions cacheEntryOptions)
    {
        IEnumerable<MenuItemEntity> entities;

        if (Name.HasValue())
        {
            var db = Services.DbContext;
            var store = Services.StoreContext.CurrentStore;
            var customerRoleIds = Services.WorkContext.CurrentCustomer.GetRoleIds();

            var menuItemQuery = db.Menus
                .ApplyStandardFilter(Name, null, store.Id, customerRoleIds)
                .ApplyMenuItemFilter(store.Id, customerRoleIds)
                .Include(x => x.Menu);

            entities = await menuItemQuery.ToListAsync();
        }
        else
        {
            entities = [];
        }

        var tree = await entities.GetTreeAsync("DatabaseMenu", _menuItemProviders);

        return tree;
    }

    protected override string GetCacheKey()
    {
        var cacheKey = "{0}-{1}-{2}".FormatInvariant(
            Services.WorkContext.WorkingLanguage.Id,
            QuerySettings.IgnoreMultiStore ? 0 : Services.StoreContext.CurrentStore.Id,
            QuerySettings.IgnoreAcl ? "0" : Services.WorkContext.CurrentCustomer.GetRolesIdent());

        return cacheKey;
    }
}