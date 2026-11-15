using OmmCMS.Core.Catalog.Brands;
using OmmCMS.Core.Catalog.Categories;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Data;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Stores;
using OmmCMS.Data.Hooks;

namespace OmmCMS.Core.Content.Menus.Hooks;

internal partial class DefaultLinkInvalidator : AsyncDbSaveHook<BaseEntity>
{
    private readonly SmartDbContext _db;
    private readonly ILinkResolver _linkResolver;

    private static readonly HashSet<string> _toxicProps = new()
    {
        nameof(Topic.SystemName),
        nameof(Topic.IsPublished),
        nameof(Topic.SubjectToAcl),
        nameof(Topic.LimitedToStores),
        nameof(Topic.Title),
        nameof(Topic.ShortTitle),
        nameof(Topic.Intro),
        nameof(StoreMapping.StoreId),
        nameof(UrlRecord.IsActive),
        nameof(UrlRecord.Slug)
    };

    public DefaultLinkInvalidator(SmartDbContext db, ILinkResolver linkResolver)
    {
        _db = db;
        _linkResolver = linkResolver;
    }

    public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
    {
        var e = entry.Entity;

        var evict = e is Topic || e is StoreMapping || e is UrlRecord;
        if (!evict)
            return HookResult.Void; // Perf

        if (evict && entry.InitialState == OmmCMS.Data.EntityState.Modified)
        {
            var modProps = entry.Entry.GetModifiedProperties();
            evict = modProps.Keys.Any(x => _toxicProps.Contains(x));
        }

        if (!evict)
        {
            return HookResult.Ok;
        }

        int? evictTopicId = null;

        if (e is Topic t)
        {
            _linkResolver.InvalidateLink("topic", t.Id);
            _linkResolver.InvalidateLink("topic", t.SystemName);
        }
        else if (e is UrlRecord ur)
        {
            _linkResolver.InvalidateLink(ur.EntityName.ToLowerInvariant(), ur.EntityId);
            evictTopicId = ur.EntityId;
        }
        else if (e is StoreMapping sm)
        {
            _linkResolver.InvalidateLink(sm.EntityName.ToLowerInvariant(), sm.EntityId);
            evictTopicId = sm.EntityId;
        }

        if (evictTopicId.HasValue)
        {
            var systemName = await _db.Set<Topic>()
                .Where(x => x.Id == evictTopicId.Value)
                .Select(x => x.SystemName)
                .FirstOrDefaultAsync(cancellationToken: cancelToken);

            if (systemName.HasValue())
            {
                _linkResolver.InvalidateLink("topic", systemName);
            }
        }

        return HookResult.Ok;
    }
}