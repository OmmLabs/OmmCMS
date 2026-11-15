using OmmCMS.Core.OutputCache;

namespace OmmCMS.DevTools;

internal sealed class CacheableRoutes : ICacheableRouteProvider
{
    public int Order => 0;

    public IEnumerable<string> GetCacheableRoutes()
        => ["vc:OmmCMS.DevTools/CustomFacet"];
}