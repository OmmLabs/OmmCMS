using System.Runtime.CompilerServices;
using OmmCMS.Core.Data;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Stores;

namespace OmmCMS.Core.Catalog.Brands;

public partial class ManufacturerService : IManufacturerService
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;

    public ManufacturerService(
        SmartDbContext db,
        IWorkContext workContext,
        IStoreContext storeContext)
    {
        _db = db;
        _workContext = workContext;
        _storeContext = storeContext;
    }

    public virtual async Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdsAsync(int[] productIds, bool includeHidden = false)
    {
        Guard.NotNull(productIds);

        if (productIds.Length == 0)
        {
            return new List<ProductManufacturer>();
        }

        var storeId = includeHidden ? 0 : _storeContext.CurrentStore.Id;
        var customerRoleIds = includeHidden ? null : _workContext.CurrentCustomer.GetRoleIds();

        var manufacturersQuery = _db.Manufacturers
            .AsNoTracking()
            .ApplyStandardFilter(includeHidden, customerRoleIds, storeId);

        // INFO: Never use .ThenInclude(x => x.MediaFile) here! It will make things painfully slow (or even unusable) under certain circumstances.
        var productManufacturerQuery = _db.ProductManufacturers
            .AsNoTracking()
            .Include(x => x.Manufacturer);

        var query =
            from pm in productManufacturerQuery
            join m in manufacturersQuery on pm.ManufacturerId equals m.Id
            where productIds.Contains(pm.ProductId)
            orderby pm.DisplayOrder
            select pm;

        return await query.ToListAsync();
    }
}
