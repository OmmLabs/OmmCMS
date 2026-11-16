using Microsoft.EntityFrameworkCore;
using OmmCMS.Core.Data;
using OmmCMS.Core.Seo;

namespace OmmCMS.Core.Content.Blogs;

/// <summary>Namespaced URL records keep blog paths distinct from root CMS pages and other blog content types.</summary>
public static class BlogSlugPaths
{
    public static bool IsManaged(string entityName) => Prefix(entityName) != null;

    private static string Prefix(string entityName) => entityName switch
    {
        nameof(BlogPost) => "blog/",
        nameof(BlogCategory) => "blog/category/",
        nameof(BlogTag) => "blog/tag/",
        _ => null
    };

    public static string GetPath(string entityName, string slug)
    {
        var prefix = Prefix(entityName) ?? throw new ArgumentException("Unsupported blog entity.", nameof(entityName));
        if (string.IsNullOrEmpty(slug) || BlogQueries.NormalizeSlug(slug) != slug)
            throw new ArgumentException("The slug must be a normalized, nonempty path segment.", nameof(slug));
        return prefix + slug;
    }

    public static string GetSlug(string entityName, string path)
    {
        var prefix = Prefix(entityName);
        if (prefix == null || path == null || !path.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var slug = path[prefix.Length..];
        return !string.IsNullOrEmpty(slug) && BlogQueries.NormalizeSlug(slug) == slug ? slug : null;
    }
}

public sealed record BlogSlugResolution(int EntityId, string Slug, bool IsRedirect);

public sealed class BlogSlugConflictException : InvalidOperationException
{
    public BlogSlugConflictException() : base("This URL belongs to another content item or its history.") { }
}

/// <summary>Coordinates canonical blog paths and their history through the central URL record service.</summary>
public class BlogSlugService
{
    private readonly SmartDbContext _db;
    private readonly IUrlService _urlService;

    public BlogSlugService(SmartDbContext db, IUrlService urlService)
    {
        _db = db;
        _urlService = urlService;
    }

    public async Task<bool> IsAvailableAsync(string entityName, int entityId, string slug)
    {
        var path = BlogSlugPaths.GetPath(entityName, slug);
        return !await _db.UrlRecords.AnyAsync(x => x.Slug == path &&
            (x.EntityName != entityName || x.EntityId != entityId || x.LanguageId != 0 || entityId == 0));
    }

    /// <summary>Called inside the content save transaction, after the entity has an assigned ID.</summary>
    public async Task SaveAsync(ISlugSupported entity, string slug)
    {
        var entityName = entity.GetEntityName();
        var path = BlogSlugPaths.GetPath(entityName, slug);
        var existing = await _db.UrlRecords.FirstOrDefaultAsync(x => x.Slug == path);
        var isSelf = existing != null && existing.EntityName == entityName && existing.EntityId == entity.Id && existing.LanguageId == 0;
        if (existing != null && !isSelf)
            throw new BlogSlugConflictException();

        // Our validated full path deliberately bypasses the generic single-segment slugifier.
        // The central service handles reactivation and deactivation of the previous URL.
        await _urlService.ApplySlugAsync(new ValidateSlugResult
        {
            Source = entity,
            Slug = path,
            Found = existing,
            FoundIsSelf = isSelf,
            LanguageId = 0,
            WasValidated = true
        }, true);
    }

    public async Task<BlogSlugResolution> ResolveAsync(string entityName, string requestedSlug)
    {
        if (string.IsNullOrEmpty(requestedSlug))
            return null;

        var slug = requestedSlug.ToLowerInvariant();
        if (BlogQueries.NormalizeSlug(slug) != slug)
            return null;

        var path = BlogSlugPaths.GetPath(entityName, slug);
        var record = await _db.UrlRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == path
            && x.EntityName == entityName && x.LanguageId == 0);
        if (record == null)
            return null;

        var canonicalPath = record.IsActive ? record.Slug : await _urlService.GetActiveSlugAsync(record.EntityId, entityName, 0);
        var canonicalSlug = BlogSlugPaths.GetSlug(entityName, canonicalPath);
        return canonicalSlug == null ? null : new BlogSlugResolution(record.EntityId, canonicalSlug,
            !record.IsActive || !string.Equals(requestedSlug, canonicalSlug, StringComparison.Ordinal));
    }
}
