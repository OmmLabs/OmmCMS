using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Seo.Routing;
using OmmCMS.Core.Stores;
using OmmCMS.Web.Infrastructure.Hooks;
using OmmCMS.Web.Models.Topics;

namespace OmmCMS.Web.Controllers;

public class TopicController : PublicController
{
    private readonly SmartDbContext _db;
    private readonly IAclService _aclService;
    private readonly IStoreMappingService _storeMappingService;
    private readonly SeoSettings _seoSettings;
    private readonly IBreadcrumb _breadcrumb;
    private readonly IMenuService _menuService;

    public TopicController(
        SmartDbContext db,
        IAclService aclService,
        IStoreMappingService storeMappingService,
        SeoSettings seoSettings,
        IBreadcrumb breadcrumb,
        IMenuService menuService)
    {
        _db = db;
        _aclService = aclService;
        _storeMappingService = storeMappingService;
        _seoSettings = seoSettings;
        _breadcrumb = breadcrumb;
        _menuService = menuService;
    }

    public async Task<IActionResult> TopicDetails(int topicId, bool popup = false)
    {
        var menu = await _menuService.GetMenuAsync("Main");
        if (menu != null)
        {
            var currentNode = await menu.ResolveCurrentNodeAsync(ControllerContext);
            currentNode?.Trail.Where(x => !x.IsRoot).Each(x => _breadcrumb.Track(x.Value));
        }

        var cacheKey = string.Format(ModelCacheInvalidator.TOPIC_BY_ID_KEY,
            topicId,
            Services.WorkContext.WorkingLanguage.Id,
            Services.StoreContext.CurrentStore.Id,
            Services.WorkContext.CurrentCustomer.GetRolesIdent());

        var cacheModel = await Services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
        {
            o.ExpiresIn(TimeSpan.FromDays(1));

            var topic = await _db.Topics.FindByIdAsync(topicId, false);
            if (topic != null
                && topic.IsPublished
                && await _storeMappingService.AuthorizeAsync(topic)
                && await _aclService.AuthorizeAsync(topic))
            {
                return PrepareTopicModel(topic, !topic.IsPasswordProtected);
            }

            return null;
        });

        if (cacheModel == null || (!popup && cacheModel.RenderAsWidget))
        {
            return NotFound();
        }

        ViewBag.IsPopup = popup;

        if (!cacheModel.RenderAsWidget)
        {
            Services.DisplayControl.Announce(new Topic { Id = cacheModel.Id });
        }

        return View(cacheModel);
    }

    [HttpPost, DisallowRobot]
    public async Task<IActionResult> Authenticate(int id, string password)
    {
        var success = false;
        var message = string.Empty;
        var content = string.Empty;
        var topic = await _db.Topics.FindByIdAsync(id, false);

        if (topic != null
            && topic.IsPublished
            && topic.IsPasswordProtected
            && await _storeMappingService.AuthorizeAsync(topic)
            && await _aclService.AuthorizeAsync(topic))
        {
            if (topic.Password != null && password != null && password == topic.Password)
            {
                var model = PrepareTopicModel(topic, true);
                content = await InvokePartialViewAsync("_Topic", model);
                success = true;
            }
            else
            {
                message = T("Topic.WrongPassword");
            }
        }
        else
        {
            message = T("Common.PageNotFound");
        }

        return Json(new { success, message, content });
    }

    protected TopicModel PrepareTopicModel(Topic topic, bool includePasswordProtectedFields)
    {
        Guard.NotNull(topic);

        var titleTag = "h3";
        if (topic.TitleTag != null)
        {
            titleTag = topic.TitleTag;
        }
        else if (!topic.RenderAsWidget)
        {
            titleTag = "h1";
        }

        var model = new TopicModel
        {
            Id = topic.Id,
            SystemName = topic.SystemName,
            HtmlId = topic.HtmlId,
            BodyCssClass = topic.BodyCssClass,
            EnableProseContainer = topic.EnableProseContainer,
            IsPasswordProtected = topic.IsPasswordProtected,
            ShortTitle = includePasswordProtectedFields ? topic.GetLocalized(x => x.ShortTitle) : null,
            Title = includePasswordProtectedFields ? topic.GetLocalized(x => x.Title) : null,
            Intro = includePasswordProtectedFields ? topic.GetLocalized(x => x.Intro) : null,
            Body = includePasswordProtectedFields ? topic.GetLocalized(x => x.Body, detectEmptyHtml: true) : null,
            MetaKeywords = topic.GetLocalized(x => x.MetaKeywords),
            MetaDescription = topic.GetLocalized(x => x.MetaDescription),
            MetaTitle = topic.GetLocalized(x => x.MetaTitle),
            SeName = topic.GetActiveSlug(),
            TitleTag = titleTag,
            RenderAsWidget = topic.RenderAsWidget
        };

        if (_seoSettings.CanonicalUrlsEnabled)
        {
            model.CanonicalUrl = Url.RouteUrl("Topic", new { model.SeName }, Request.Scheme);
        }

        return model;
    }
}