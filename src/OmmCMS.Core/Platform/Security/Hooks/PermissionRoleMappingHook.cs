using OmmCMS.Caching;
using OmmCMS.Core.Data;
using OmmCMS.Data.Hooks;

namespace OmmCMS.Core.Security;

[Important]
[ServiceLifetime(ServiceLifetime.Singleton)]
internal class PermissionRoleMappingHook : AsyncDbSaveHook<PermissionRoleMapping>
{
    private readonly ICacheManager _cache;

    public PermissionRoleMappingHook(ICacheManager cache)
    {
        _cache = cache;
    }

    public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        await _cache.RemoveByPatternAsync(PermissionService.PERMISSION_TREE_PATTERN_KEY);
    }
}