using OmmCMS.Caching;
using OmmCMS.Core.Data;
using OmmCMS.Data.Hooks;

namespace OmmCMS.Core.Catalog.Discounts;

internal class DiscountUsageHistoryHook : AsyncDbSaveHook<DiscountUsageHistory>
{
    private readonly IRequestCache _requestCache;

    public DiscountUsageHistoryHook(IRequestCache requestCache)
    {
        _requestCache = requestCache;
    }

    public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        _requestCache.RemoveByPattern(DiscountService.DiscountsPatternKey);

        return Task.CompletedTask;
    }
}