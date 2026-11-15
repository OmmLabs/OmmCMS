using OmmCMS.Core.Data;
using OmmCMS.Data.Hooks;

namespace OmmCMS.Core.Catalog.Products;

[Important]
internal class ProductReviewHook : AsyncDbSaveHook<ProductReview>
{
    private readonly SmartDbContext _db;
    private readonly Lazy<IProductService> _productService;
    private int[] _productIdsToUpdate;

    public ProductReviewHook(SmartDbContext db, Lazy<IProductService> productService)
    {
        _db = db;
        _productService = productService;
    }

    protected override Task<HookResult> OnDeletingAsync(ProductReview entity, IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    protected override Task<HookResult> OnDeletedAsync(ProductReview entity, IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        var deletedReviews = entries
            .Where(x => x.State == EntityState.Deleted)
            .Select(x => x.Entity)
            .OfType<ProductReview>()
            .ToArray();
        if (deletedReviews.Length > 0)
        {
            _productIdsToUpdate = deletedReviews.ToDistinctArray(x => x.ProductId);
        }
        return Task.CompletedTask;
    }

    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        if (!_productIdsToUpdate.IsNullOrEmpty())
        {
            foreach (var productIdsChunk in _productIdsToUpdate.Chunk(100))
            {
                // Avoid loading tracked entities (too slow) and apply stats on stub objects.
                var productStubs = productIdsChunk
                    .Select(id => new Product { Id = id })
                    .ToList();
                await _productService.Value.ApplyProductReviewTotalsAsync(productStubs, cancelToken);

                foreach (var stub in productStubs)
                {
                    await _db.Products
                        .Where(x => x.Id == stub.Id)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.ApprovedRatingSum, stub.ApprovedRatingSum)
                            .SetProperty(x => x.NotApprovedRatingSum, stub.NotApprovedRatingSum)
                            .SetProperty(x => x.ApprovedTotalReviews, stub.ApprovedTotalReviews)
                            .SetProperty(x => x.NotApprovedTotalReviews, stub.NotApprovedTotalReviews),
                            cancelToken);
                }
            }
        }
    }
}
