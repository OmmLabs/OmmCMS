using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Seo.Routing;

namespace OmmCMS.Core.Content.Blogs;

public class BlogXmlSitemapPublisher : IXmlSitemapPublisher
{
    private readonly SmartDbContext _db;

    public BlogXmlSitemapPublisher(SmartDbContext db)
        => _db = db;

    public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
    {
        if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesBlog)
            return null;

        var now = DateTime.UtcNow;
        return new Provider
        {
            Query = _db.BlogPosts.AsNoTracking().Where(x => x.IncludeInSitemap && x.IsPublished
                && x.PublishedOnUtc <= now && (x.ExpiresOnUtc == null || x.ExpiresOnUtc > now)
                && (x.StoreId == 0 || x.StoreId == context.Store.Id))
        };
    }

    private sealed class Provider : XmlSitemapProvider
    {
        public IQueryable<BlogPost> Query { get; init; }
        public override int Order => 210;
        public override Task<int> GetTotalCountAsync() => Query.CountAsync();

        public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            var posts = await Query.Select(x => new { x.Id, x.Slug, x.UpdatedOnUtc, x.LanguageId }).ToListAsync(cancelToken);
            foreach (var p in posts)
                yield return new NamedEntity { Id = p.Id, EntityName = nameof(BlogPost), LastMod = p.UpdatedOnUtc, LanguageId = p.LanguageId };
        }

        public override XmlSitemapNode CreateNode(string baseUrl, NamedEntity entity, UrlRecordCollection slugs,
            Language language, XmlSitemapBuildNodeContext ctx)
        {
            var path = slugs.GetSlug(0, entity.Id, true);
            return BlogSlugPaths.GetSlug(nameof(BlogPost), path) == null
                ? null
                : new XmlSitemapNode { Loc = baseUrl.TrimEnd('/') + "/" + RouteHelper.NormalizePathComponent(path), LastMod = entity.LastMod };
        }
    }
}
