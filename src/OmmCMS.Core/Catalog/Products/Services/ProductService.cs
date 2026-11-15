using System.Collections.Immutable;
using Autofac;
using OmmCMS.Caching;
using OmmCMS.Collections;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Brands;
using OmmCMS.Core.Catalog.Categories;
using OmmCMS.Core.Common;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Data;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Messaging;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Stores;
using OmmCMS.Data;
using OmmCMS.Events;

namespace OmmCMS.Core.Catalog.Products;

public partial class ProductService : IProductService
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheManager _cache;
    private readonly IComponentContext _componentContext;
    private readonly Lazy<IProductTagService> _productTagService;
    private readonly IUrlService _urlService;

    public ProductService(
        SmartDbContext db,
        IWorkContext workContext,
        IStoreContext storeContext,
        IEventPublisher eventPublisher,
        ICacheManager cache,
        IComponentContext componentContext,
        Lazy<IProductTagService> productTagService,
        IUrlService urlService)
    {
        _db = db;
        _workContext = workContext;
        _storeContext = storeContext;
        _eventPublisher = eventPublisher;
        _cache = cache;
        _componentContext = componentContext;
        _productTagService = productTagService;
        _urlService = urlService;
    }

    public ILogger Logger { get; set; } = NullLogger.Instance;
    public Localizer T { get; set; } = NullLocalizer.Instance;

    public virtual async Task<(Product Product, ProductVariantAttributeCombination VariantCombination)> GetProductByCodeAsync(
        string code,
        bool includeHidden = false,
        bool tracked = false)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (null, null);
        }

        var pq = _db.Products
            .ApplyTracking(tracked)
            .ApplyStandardFilter(includeHidden);

        if (!includeHidden)
        {
            pq = pq.Where(x => x.Visibility <= ProductVisibility.SearchResults);
        }

        var product = await pq
            .ApplyProductCodeFilter(code)
            .FirstOrDefaultAsync();

        if (product != null)
        {
            return (product, null);
        }

        var pvcq = _db.ProductVariantAttributeCombinations
            .Include(x => x.Product)
            .ApplyTracking(tracked)
            .ApplyStandardFilter(includeHidden)
            .ApplyProductCodeFilter(code);

        if (!includeHidden)
        {
            pvcq = pvcq.Where(x => x.Product.Visibility <= ProductVisibility.SearchResults);
        }

        var variantCombination = await pvcq.FirstOrDefaultAsync();

        return (variantCombination?.Product, variantCombination);
    }

    public virtual async Task<Multimap<int, ProductTag>> GetProductTagsByProductIdsAsync(int[] productIds, bool includeHidden = false)
    {
        Guard.NotNull(productIds);

        var map = new Multimap<int, ProductTag>();
        if (!productIds.Any())
        {
            return map;
        }

        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.ProductTags)
            .Where(x => productIds.Contains(x.Id))
            .ApplyStandardFilter(includeHidden);

        if (!includeHidden)
        {
            // Only tags of products that are fully visible.
            query = query.Where(x => x.Visibility == ProductVisibility.Full);
        }

        var items = await query
            .Select(x => new
            {
                ProductId = x.Id,
                Tags = x.ProductTags.Where(y => includeHidden || y.Published)
            })
            .ToListAsync();

        foreach (var item in items)
        {
            map.AddRange(item.ProductId, item.Tags);
        }

        return map;
    }

    public virtual ProductBatchContext CreateProductBatchContext(
        IEnumerable<Product> products = null,
        Store store = null,
        Customer customer = null,
        bool includeHidden = true,
        bool loadMainMediaOnly = false)
    {
        return new ProductBatchContext(
            products,
            _db,
            _componentContext,
            store ?? _storeContext.CurrentStore,
            customer ?? _workContext.CurrentCustomer,
            includeHidden,
            loadMainMediaOnly);
    }

    #region Recycle bin

    public virtual async Task<int> RestoreProductsAsync(int[] productIds, bool? publishAfterRestore = null, CancellationToken cancelToken = default)
    {
        if (productIds.IsNullOrEmpty())
        {
            return 0;
        }

        var success = 0;
        var parentCategories = await _db.Categories
            .IgnoreQueryFilters()
            .Select(x => new { x.Id, x.ParentId })
            .ToDictionaryAsync(x => x.Id, x => x.ParentId ?? 0, cancelToken);

        foreach (var productId in productIds)
        {
            try
            {
                await RestoreProductInternal(productId, publishAfterRestore, parentCategories, cancelToken);
                success++;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        if (success > 0)
        {
            await _productTagService.Value.ClearCacheAsync();
            await _cache.RemoveByPatternAsync(CategoryService.CategoryTreePatternKey);
            await _eventPublisher.PublishAsync(new CategoryTreeChangedEvent(CategoryTreeChangeReason.Hierarchy), cancelToken);
            await _eventPublisher.PublishAsync(new CategoryTreeChangedEvent(CategoryTreeChangeReason.ElementCounts), cancelToken);
        }

        return success;
    }

    private async Task RestoreProductInternal(
        int productId,
        bool? publishAfterRestore,
        Dictionary<int, int> parentCategories,
        CancellationToken cancelToken)
    {
        var productIds = new HashSet<int> { productId };
        var now = DateTime.UtcNow;

        // Add IDs of related products to be restored as well.
        productIds.AddRange(await _db.ProductBundleItem
            .IgnoreQueryFilters()
            .Where(x => x.BundleProductId == productId && x.Product.Deleted)
            .Select(x => x.ProductId)
            .ToArrayAsync(cancelToken));

        // First restore products, then all other entities.
        // Allow hooks to react (e.g. for updating search index).
        var products = await _db.Products
            .IgnoreQueryFilters()
            .Where(x => productIds.Contains(x.Id) && x.Deleted)
            .ToListAsync(cancelToken);

        foreach (var product in products)
        {
            product.Deleted = false;

            if (publishAfterRestore.HasValue)
            {
                product.Published = publishAfterRestore.Value;
            }
        }

        await _db.SaveChangesAsync(cancelToken);

        var updatedProductIds = products.Select(x => x.Id).ToArray();

        // Check if slugs are missing.
        var productIdsWithMissingSlug = updatedProductIds.Except(await _db.UrlRecords
            .Where(x => updatedProductIds.Contains(x.EntityId) && x.EntityName == nameof(Product) && x.IsActive)
            .Select(x => x.EntityId)
            .ToArrayAsync(cancelToken)).ToArray();

        var productsWithMissingSlug = products
            .Where(x => productIdsWithMissingSlug.Contains(x.Id))
            .ToArray();

        if (productIdsWithMissingSlug.Length > 0)
        {
            foreach (var product in productsWithMissingSlug)
            {
                var validateSlugResult = await _urlService.SaveSlugAsync(product, null, product.Name, true);
            }

            await _db.SaveChangesAsync(cancelToken);
        }

        // Restore manufacturers.
        var deletedManufacturerIdsQuery = _db.ProductManufacturers
            .IgnoreQueryFilters()
            .Where(x => updatedProductIds.Contains(x.ProductId) && x.Manufacturer.Deleted)
            .Select(x => x.ManufacturerId);

        await _db.Manufacturers
            .IgnoreQueryFilters()
            .Where(x => deletedManufacturerIdsQuery.Contains(x.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Deleted, false)
                .SetProperty(x => x.UpdatedOnUtc, now)
                .SetProperty(x => x.Published, x => publishAfterRestore != null ? publishAfterRestore.Value : x.Published),
                cancelToken);

        // Restore categories.
        var restoreCategoryIds = new HashSet<int>();
        var assignedCategoryIds = await _db.ProductCategories
            .IgnoreQueryFilters()
            .Where(x => updatedProductIds.Contains(x.ProductId))
            .Select(x => x.CategoryId)
            .Distinct()
            .ToArrayAsync(cancelToken);
        assignedCategoryIds.Each(GetCategoryIds);

        if (restoreCategoryIds.Count > 0)
        {
            await _db.Categories
                .IgnoreQueryFilters()
                .Where(x => restoreCategoryIds.Contains(x.Id) && x.Deleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Deleted, false)
                    .SetProperty(x => x.UpdatedOnUtc, now)
                    .SetProperty(x => x.Published, x => publishAfterRestore != null ? publishAfterRestore.Value : x.Published),
                    cancelToken);
        }

        void GetCategoryIds(int categoryId)
        {
            if (categoryId != 0)
            {
                restoreCategoryIds.Add(categoryId);
                if (parentCategories.TryGetValue(categoryId, out var parentId))
                {
                    GetCategoryIds(parentId);
                }
            }
        }
    }

    public virtual async Task<DeletionResult> DeleteProductsPermanentAsync(int[] productIds, CancellationToken cancelToken = default)
    {
        var result = new DeletionResult();

        if (productIds.IsNullOrEmpty())
        {
            return result;
        }

        foreach (var productIdsChunk in productIds.Chunk(50))
        {
            var deletionEvent = new PermanentDeletionRequestedEvent<Product>(productIdsChunk);

            try
            {
                await _eventPublisher.PublishAsync(deletionEvent, cancelToken);

                var idsToDelete = deletionEvent.DisallowedEntityIds.Count > 0
                    ? productIds.Except(deletionEvent.DisallowedEntityIds).ToArray()
                    : productIdsChunk;

                await DeleteProductsPermanentInternal(idsToDelete, cancelToken);
                result.DeletedRecords += idsToDelete.Length;
                result.Errors.AddRange(deletionEvent.Errors);

                foreach (var entitiesDeleted in deletionEvent.EntitiesDeletedCallbacks)
                {
                    try
                    {
                        await entitiesDeleted(cancelToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        result.Errors.Add(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                result.Errors.Add(ex.Message);
            }
            finally
            {
                deletionEvent.EntitiesDeletedCallbacks.Clear();
            }
        }

        return result;
    }

    private async Task DeleteProductsPermanentInternal(int[] productIds, CancellationToken cancelToken)
    {
        // INFO: not necessary here to check whether an assignment is of certain type,
        // (e.g. if the deleted product is grouped or bundled product). Just remove any assignment.

        const string entityName = nameof(Product);

        // ----- Product assignments.

        await _db.Products
            .IgnoreQueryFilters()
            .Where(x => productIds.Contains(x.ParentGroupedProductId))
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.ParentGroupedProductId, p => 0), cancelToken);


        // ----- Missing cascade delete rules.
        // Avoids "The DELETE statement conflicted with the REFERENCE constraint...".

        await _db.ProductBundleItem
            .Where(x => productIds.Contains(x.ProductId))
            .ExecuteDeleteAsync(cancelToken);

        await _db.Downloads
            .Where(x => productIds.Contains(x.EntityId) && x.EntityName == entityName)
            .ExecuteDeleteAsync(cancelToken);

        // ----- Satellite tables.

        await _db.StoreMappings
            .Where(x => productIds.Contains(x.EntityId) && x.EntityName == entityName)
            .ExecuteDeleteAsync(cancelToken);

        await _db.AclRecords
            .Where(x => productIds.Contains(x.EntityId) && x.EntityName == entityName)
            .ExecuteDeleteAsync(cancelToken);

        await _db.UrlRecords
            .Where(x => productIds.Contains(x.EntityId) && x.EntityName == entityName)
            .ExecuteDeleteAsync(cancelToken);

        await _db.LocalizedProperties
            .Where(x => productIds.Contains(x.EntityId) && x.LocaleKeyGroup == entityName)
            .ExecuteDeleteAsync(cancelToken);

        await _db.GenericAttributes
            .Where(x => productIds.Contains(x.EntityId) && x.KeyGroup == entityName)
            .ExecuteDeleteAsync(cancelToken);

        await _db.SyncMappings
            .Where(x => productIds.Contains(x.EntityId) && x.EntityName == entityName)
            .ExecuteDeleteAsync(cancelToken);

        // FROM [MediaTrack] AS [m]
        // WHERE EXISTS (
        //    SELECT 1 FROM [Product_MediaFile_Mapping] AS [p]
        //    WHERE [p].[ProductId] IN (...) AND [p].[Id] = [m].[EntityId]) AND [m].[EntityName] = N'ProductMediaFile'

        var productMediaFileIdsQuery = _db.ProductMediaFiles
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => x.Id);

        await _db.MediaTracks
            .Where(x => productMediaFileIdsQuery.Contains(x.EntityId) && x.EntityName == NamedEntity.GetEntityName<ProductMediaFile>())
            .ExecuteDeleteAsync(cancelToken);

        var attributeValuesIdsQuery = _db.ProductVariantAttributeValues
            .Where(x => productIds.Contains(x.ProductVariantAttribute.ProductId))
            .Select(x => x.Id);

        await _db.MediaTracks
            .Where(x => attributeValuesIdsQuery.Contains(x.EntityId) && x.EntityName == NamedEntity.GetEntityName<ProductVariantAttributeValue>())
            .ExecuteDeleteAsync(cancelToken);

        // ----- Finally delete products.
        // If we have forgotten something, this will lead to an exception.
        await _db.Products
            .IgnoreQueryFilters()
            .Where(x => productIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancelToken);
    }

    #endregion
}