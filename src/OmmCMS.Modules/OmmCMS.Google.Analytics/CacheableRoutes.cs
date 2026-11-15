using OmmCMS.Core.OutputCache;

namespace OmmCMS.Google.Analytics;

internal sealed class CacheableRoutes : ICacheableRouteProvider
{
    public int Order => 0;

    public IEnumerable<string> GetCacheableRoutes() => ["vc:OmmCMS.Google.Analytics/GoogleAnalytics"];
}