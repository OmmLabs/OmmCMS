using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmmCMS.Core.Data;
using System.Xml.Linq;
using OmmCMS.Core.Content.Blogs;
using OmmCMS.Core.Localization.Routing;
using OmmCMS.Web.Models.Blogs;

namespace OmmCMS.Web.Controllers;

public class BlogController : PublicController
{
    private readonly SmartDbContext _db;

    private readonly BlogSlugService _slugs;

    public BlogController(SmartDbContext db, BlogSlugService slugs)
    {
        _db = db;
        _slugs = slugs;
    }

    private IQueryable<BlogPost> VisiblePosts() => _db.BlogPosts.AsNoTracking()
        .Visible(DateTime.UtcNow, Services.StoreContext.CurrentStore.Id, Services.WorkContext.WorkingLanguage.Id);

    [HttpGet, LocalizedRoute("blog", Name = "BlogHome")]
    public async Task<IActionResult> Index(string q, int page = 1)
        => await RenderList(q, page);

    [HttpGet, LocalizedRoute("blog/category/{slug}", Name = "BlogCategory")]
    public async Task<IActionResult> Category(string slug, string q, int page = 1)
    {
        var resolved = await _slugs.ResolveAsync(nameof(BlogCategory), slug);
        if (resolved == null || !await _db.BlogCategories.AnyAsync(x => x.Id == resolved.EntityId))
            return NotFound();
        if (resolved.IsRedirect)
            return RedirectToRoutePermanent("BlogCategory", new { slug = resolved.Slug, q, page });
        return await RenderList(q, page, category: resolved.Slug);
    }

    [HttpGet, LocalizedRoute("blog/tag/{slug}", Name = "BlogTag")]
    public async Task<IActionResult> Tag(string slug, string q, int page = 1)
    {
        var resolved = await _slugs.ResolveAsync(nameof(BlogTag), slug);
        if (resolved == null || !await _db.BlogTags.AnyAsync(x => x.Id == resolved.EntityId))
            return NotFound();
        if (resolved.IsRedirect)
            return RedirectToRoutePermanent("BlogTag", new { slug = resolved.Slug, q, page });
        return await RenderList(q, page, tag: resolved.Slug);
    }

    [HttpGet, LocalizedRoute("blog/archive/{year:int}/{month:int}", Name = "BlogArchive")]
    public async Task<IActionResult> Archive(int year, int month, int page = 1)
        => await RenderList(null, page, year: year, month: month);

    [HttpGet, LocalizedRoute("blog/{slug}", Name = "BlogPost")]
    public async Task<IActionResult> Detail(string slug)
    {
        var resolved = await _slugs.ResolveAsync(nameof(BlogPost), slug);
        if (resolved == null) return NotFound();

        var visible = VisiblePosts();
        var post = await visible.Include(x => x.Categories).Include(x => x.Tags).AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == resolved.EntityId);
        if (post == null) return NotFound();
        if (resolved.IsRedirect)
            return RedirectToRoutePermanent("BlogPost", new { slug = resolved.Slug });

        var categories = post.Categories.Select(x => x.Id).ToArray();
        var tags = post.Tags.Select(x => x.Id).ToArray();
        var related = await visible.Where(x => x.Id != post.Id &&
            (x.Categories.Any(t => categories.Contains(t.Id)) || x.Tags.Any(t => tags.Contains(t.Id))))
            .OrderByDescending(x => x.PublishedOnUtc).ThenByDescending(x => x.Id).Take(4).ToListAsync();
        return View(new BlogDetailModel { Post = post, Related = related });
    }

    [HttpGet, LocalizedRoute("blog/rss.xml", Name = "BlogRss")]
    public async Task<IActionResult> Rss()
    {
        var posts = await VisiblePosts().OrderByDescending(x => x.PublishedOnUtc).ThenByDescending(x => x.Id)
            .Take(30).ToListAsync();
        var home = Url.RouteUrl("BlogHome", null, Request.Scheme);
        var xml = new XDocument(new XElement("rss", new XAttribute("version", "2.0"),
            new XElement("channel", new XElement("title", "Blog"), new XElement("link", home),
                new XElement("description", "Latest articles"),
                posts.Select(p =>
                {
                    var link = Url.RouteUrl("BlogPost", new { p.Slug }, Request.Scheme);
                    return new XElement("item", new XElement("title", p.Title), new XElement("link", link),
                        new XElement("guid", link), new XElement("description", p.Intro ?? ""),
                        new XElement("pubDate", DateTime.SpecifyKind(p.PublishedOnUtc, DateTimeKind.Utc).ToString("R")));
                }))));
        return Content(xml.ToString(), "application/rss+xml", Encoding.UTF8);
    }

    private async Task<IActionResult> RenderList(string q, int page, string category = null, string tag = null,
        int? year = null, int? month = null)
    {
        if (page < 1 || page > 100000) return NotFound();
        var all = VisiblePosts();
        var model = new BlogPageModel { Query = q, Page = page, Category = category, Tag = tag, Year = year, Month = month };
        var query = all;
        if (category != null)
        {
            var term = await _db.BlogCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == category);
            if (term == null) return NotFound();
            model.Title = term.Name; model.Description = term.Description;
            query = query.Where(x => x.Categories.Any(t => t.Id == term.Id));
        }
        if (tag != null)
        {
            var term = await _db.BlogTags.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == tag);
            if (term == null) return NotFound();
            model.Title = term.Name; model.Description = term.Description;
            query = query.Where(x => x.Tags.Any(t => t.Id == term.Id));
        }
        if (year.HasValue && month.HasValue)
        {
            if (year < 1 || year > 9998 || month < 1 || month > 12) return NotFound();
            var start = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            model.Title = start.ToString("MMMM yyyy");
            query = query.Where(x => x.PublishedOnUtc >= start && x.PublishedOnUtc < end);
        }
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q) || x.Intro.Contains(q) || x.Body.Contains(q));

        model.Total = await query.CountAsync();
        if (page > 1 && (page - 1) * model.PageSize >= model.Total) return NotFound();
        model.Posts = await query.Include(x => x.Categories).Include(x => x.Tags).AsSplitQuery()
            .OrderByDescending(x => x.PublishedOnUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
        model.Categories = await _db.BlogCategories.AsNoTracking()
            .Where(t => all.Any(p => p.Categories.Any(c => c.Id == t.Id)))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();
        model.Tags = await _db.BlogTags.AsNoTracking()
            .Where(t => all.Any(p => p.Tags.Any(c => c.Id == t.Id)))
            .OrderBy(x => x.Name).ToListAsync();
        model.Archive = await all.GroupBy(x => new { x.PublishedOnUtc.Year, x.PublishedOnUtc.Month })
            .OrderByDescending(x => x.Key.Year).ThenByDescending(x => x.Key.Month)
            .Select(x => new BlogArchiveMonth(x.Key.Year, x.Key.Month, x.Count())).ToListAsync();
        return View("Index", model);
    }
}
