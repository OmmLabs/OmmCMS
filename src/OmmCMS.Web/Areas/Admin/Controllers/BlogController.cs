using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmmCMS.Core.Data;
using OmmCMS.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using OmmCMS.Admin.Models.Blogs;
using OmmCMS.Core.Content.Blogs;
using OmmCMS.Core.Security;

namespace OmmCMS.Admin.Controllers;

public class BlogController : AdminController
{
    private readonly SmartDbContext _db;
    private readonly IPermissionService _permissions;
    private readonly BlogSlugService _slugs;

    public BlogController(SmartDbContext db, IPermissionService permissions, BlogSlugService slugs)
    {
        _db = db;
        _permissions = permissions;
        _slugs = slugs;
    }

    [HttpGet, Permission(Permissions.Cms.Blog.Read)]
    public async Task<IActionResult> List(string q, int page = 1, bool? published = null)
    {
        page = Math.Clamp(page, 1, 100000);
        var posts = _db.BlogPosts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            posts = posts.Where(x => x.Title.Contains(q) || x.Intro.Contains(q) || x.Tags.Any(t => t.Name.Contains(q)));
        if (published.HasValue)
            posts = posts.Where(x => x.IsPublished == published.Value);
        ViewBag.Query = q;
        ViewBag.Published = published;
        ViewBag.Page = page;
        ViewBag.Total = await posts.CountAsync();
        return View(await posts.OrderByDescending(x => x.UpdatedOnUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 30).Take(30).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (!await _permissions.AuthorizeAsync(id == 0 ? Permissions.Cms.Blog.Create : Permissions.Cms.Blog.Read))
            return Forbid();

        var model = new BlogEditModel();
        if (id != 0)
        {
            var p = await _db.BlogPosts.AsNoTracking().Include(x => x.Categories).Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            model = new BlogEditModel
            {
                Id = p.Id, Title = p.Title, Slug = p.Slug, Intro = p.Intro, Body = p.Body,
                Author = p.Author, PreviewImageId = p.PreviewImageId, CoverImageId = p.CoverImageId,
                StoreId = p.StoreId, LanguageId = p.LanguageId, IsPublished = p.IsPublished,
                PublishedOnUtc = p.PublishedOnUtc, ExpiresOnUtc = p.ExpiresOnUtc,
                MetaTitle = p.MetaTitle, MetaDescription = p.MetaDescription, IncludeInSitemap = p.IncludeInSitemap,
                CategoryIds = p.Categories.Select(x => x.Id).ToArray(), TagIds = p.Tags.Select(x => x.Id).ToArray()
            };
        }
        await PrepareEditorAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BlogEditModel model)
    {
        var creating = model.Id == 0;
        if (!await _permissions.AuthorizeAsync(creating ? Permissions.Cms.Blog.Create : Permissions.Cms.Blog.Update))
            return Forbid();

        var p = creating ? new BlogPost() : await _db.BlogPosts.Include(x => x.Categories).Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == model.Id);
        if (p == null) return NotFound();

        if ((model.IsPublished || p.IsPublished) && !await _permissions.AuthorizeAsync(Permissions.Cms.Blog.Publish))
            return Forbid();

        model.Slug = BlogQueries.NormalizeSlug(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (string.IsNullOrEmpty(model.Slug))
            ModelState.AddModelError(nameof(model.Slug), "Enter a search-friendly URL slug using letters or numbers.");
        else if (!await _slugs.IsAvailableAsync(nameof(BlogPost), model.Id, model.Slug))
            ModelState.AddModelError(nameof(model.Slug), "This post URL is already in use or reserved by another post’s URL history.");

        if (model.ExpiresOnUtc <= model.PublishedOnUtc)
            ModelState.AddModelError(nameof(model.ExpiresOnUtc), "The end date must be after the publication date.");
        if (model.StoreId != 0 && !await _db.Stores.AnyAsync(x => x.Id == model.StoreId))
            ModelState.AddModelError(nameof(model.StoreId), "Unknown site.");
        if (model.LanguageId != 0 && !await _db.Languages.AnyAsync(x => x.Id == model.LanguageId))
            ModelState.AddModelError(nameof(model.LanguageId), "Unknown language.");

        var imageIds = new[] { model.PreviewImageId, model.CoverImageId }.Where(x => x.GetValueOrDefault() > 0)
            .Select(x => x.Value).Distinct().ToArray();
        if (await _db.MediaFiles.CountAsync(x => imageIds.Contains(x.Id) && !x.Deleted && !x.Hidden && x.MediaType == "image") != imageIds.Length)
            ModelState.AddModelError("", "Choose an available image from the media library.");

        var categoryIds = (model.CategoryIds ?? []).Distinct().ToArray();
        var tagIds = (model.TagIds ?? []).Distinct().ToArray();
        var categories = await _db.BlogCategories.Where(x => categoryIds.Contains(x.Id)).ToListAsync();
        var tags = await _db.BlogTags.Where(x => tagIds.Contains(x.Id)).ToListAsync();
        if (categories.Count != categoryIds.Length || tags.Count != tagIds.Length)
            ModelState.AddModelError("", "One or more categories or tags no longer exist.");

        if (ModelState.IsValid)
        {
            p.Title = model.Title.Trim();
            p.Slug = model.Slug;
            p.Intro = model.Intro;
            p.Body = BlogQueries.SanitizeBody(model.Body);
            p.Author = model.Author;
            p.PreviewImageId = model.PreviewImageId > 0 ? model.PreviewImageId : null;
            p.CoverImageId = model.CoverImageId > 0 ? model.CoverImageId : null;
            p.StoreId = model.StoreId;
            p.LanguageId = model.LanguageId;
            p.IsPublished = model.IsPublished;
            p.PublishedOnUtc = DateTime.SpecifyKind(model.PublishedOnUtc, DateTimeKind.Utc);
            p.ExpiresOnUtc = model.ExpiresOnUtc.HasValue ? DateTime.SpecifyKind(model.ExpiresOnUtc.Value, DateTimeKind.Utc) : null;
            p.MetaTitle = model.MetaTitle;
            p.MetaDescription = model.MetaDescription;
            p.IncludeInSitemap = model.IncludeInSitemap;
            p.UpdatedOnUtc = DateTime.UtcNow;
            p.Categories.Clear();
            foreach (var category in categories) p.Categories.Add(category);
            p.Tags.Clear();
            foreach (var tag in tags) p.Tags.Add(tag);
            if (creating)
            {
                p.CreatedOnUtc = p.UpdatedOnUtc;
                _db.BlogPosts.Add(p);
            }

            try
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                await _db.SaveChangesAsync();
                await _slugs.SaveAsync(p, model.Slug);
                await transaction.CommitAsync();
                NotifySuccess("Blog post saved.");
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex) when (ex is DbUpdateException or BlogSlugConflictException)
            {
                _db.ChangeTracker.Clear();
                ModelState.AddModelError("", "The post could not be saved. Check the URL and selected terms.");
            }
        }
        model.Body = BlogQueries.SanitizeBody(model.Body);
        ModelState.Remove(nameof(model.Body));
        await PrepareEditorAsync();
        return View(model);
    }

    [HttpPost, Permission(Permissions.Cms.Blog.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post == null) return NotFound();
        if (post.IsPublished && !await _permissions.AuthorizeAsync(Permissions.Cms.Blog.Publish)) return Forbid();
        _db.BlogPosts.Remove(post);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(List));
    }

    [HttpGet, Permission(Permissions.Cms.Blog.Taxonomy)]
    public async Task<IActionResult> Taxonomy(bool isTag = false)
    {
        ViewBag.IsTag = isTag;
        var terms = isTag
            ? await _db.BlogTags.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new BlogTermEditModel { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, IsTag = true }).ToListAsync()
            : await _db.BlogCategories.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                .Select(x => new BlogTermEditModel { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, DisplayOrder = x.DisplayOrder }).ToListAsync();
        return View(terms);
    }

    [HttpGet, Permission(Permissions.Cms.Blog.Taxonomy)]
    public async Task<IActionResult> Term(int id = 0, bool isTag = false)
    {
        if (id == 0) return View(new BlogTermEditModel { IsTag = isTag });
        var model = isTag
            ? await _db.BlogTags.AsNoTracking().Where(x => x.Id == id).Select(x => new BlogTermEditModel
                { Id = x.Id, IsTag = true, Name = x.Name, Slug = x.Slug, Description = x.Description }).FirstOrDefaultAsync()
            : await _db.BlogCategories.AsNoTracking().Where(x => x.Id == id).Select(x => new BlogTermEditModel
                { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, DisplayOrder = x.DisplayOrder }).FirstOrDefaultAsync();
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, Permission(Permissions.Cms.Blog.Taxonomy)]
    public async Task<IActionResult> Term(BlogTermEditModel model)
    {
        model.Slug = BlogQueries.NormalizeSlug(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug);
        if (string.IsNullOrEmpty(model.Slug))
            ModelState.AddModelError(nameof(model.Slug), "Enter a URL slug using letters or numbers.");
        else if (!await _slugs.IsAvailableAsync(model.IsTag ? nameof(BlogTag) : nameof(BlogCategory), model.Id, model.Slug))
            ModelState.AddModelError(nameof(model.Slug), "This URL is already in use or reserved by another term’s URL history.");

        if (!ModelState.IsValid) return View(model);
        OmmCMS.Core.Seo.ISlugSupported entity;
        if (model.IsTag)
        {
            var tag = model.Id == 0 ? new BlogTag() : await _db.BlogTags.FindAsync(model.Id);
            if (tag == null) return NotFound();
            tag.Name = model.Name.Trim(); tag.Slug = model.Slug; tag.Description = model.Description;
            if (model.Id == 0) _db.BlogTags.Add(tag);
            entity = tag;
        }
        else
        {
            var category = model.Id == 0 ? new BlogCategory() : await _db.BlogCategories.FindAsync(model.Id);
            if (category == null) return NotFound();
            category.Name = model.Name.Trim(); category.Slug = model.Slug;
            category.Description = model.Description; category.DisplayOrder = model.DisplayOrder;
            if (model.Id == 0) _db.BlogCategories.Add(category);
            entity = category;
        }
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();
            await _slugs.SaveAsync(entity, model.Slug);
            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or BlogSlugConflictException)
        {
            _db.ChangeTracker.Clear();
            ModelState.AddModelError("", "The term could not be saved. Check the URL.");
            return View(model);
        }
        return RedirectToAction(nameof(Taxonomy), new { model.IsTag });
    }

    [HttpPost, Permission(Permissions.Cms.Blog.Taxonomy)]
    public async Task<IActionResult> DeleteTerm(int id, bool isTag)
    {
        if (isTag)
        {
            var tag = await _db.BlogTags.FindAsync(id);
            if (tag == null) return NotFound();
            _db.BlogTags.Remove(tag);
        }
        else
        {
            var category = await _db.BlogCategories.FindAsync(id);
            if (category == null) return NotFound();
            _db.BlogCategories.Remove(category);
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Taxonomy), new { isTag });
    }

    private async Task PrepareEditorAsync()
    {
        ViewBag.Categories = new SelectList(await _db.BlogCategories.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Tags = new SelectList(await _db.BlogTags.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Stores = new SelectList(await _db.Stores.AsNoTracking().ToListAsync(), "Id", "Name");
        ViewBag.Languages = new SelectList(await _db.Languages.AsNoTracking().ToListAsync(), "Id", "Name");
        ViewBag.CanPublish = await _permissions.AuthorizeAsync(Permissions.Cms.Blog.Publish);
    }
}
