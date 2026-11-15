using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OmmCMS.Caching;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Data;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Seo.Routing;
using OmmCMS.Core.Stores;
using OmmCMS.Test.Common;

namespace OmmCMS.Core.Tests.Seo;

[TestFixture]
public class UrlServiceTests : ServiceTestBase
{
    private IUrlService _urlService;
    private Store _store;
    private Customer _customer;
    private Language _language;
    private SeoSettings _seoSettings;
    private readonly List<NamedEntity> _articles = new();

    #region Setup & Teardown

    [SetUp]
    public new void SetUp()
    {
        _store = new Store { Id = 1 };
        _customer = new Customer { Id = 1 };
        _language = new Language { Id = 1 };
        _seoSettings = new SeoSettings();

        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(x => x.CurrentStore).Returns(_store);

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.CurrentCustomer).Returns(_customer);
        workContextMock.Setup(x => x.WorkingLanguage).Returns(_language);

        var languageServiceMock = new Mock<ILanguageService>();
        var reservedSlugTableMock = new Mock<IRouteHelper>();

        _urlService = new UrlService(
            db: DbContext,
            cache: NullCache.Instance,
            httpContextAccessor: null,
            workContext: workContextMock.Object,
            storeContext: storeContextMock.Object,
            languageService: languageServiceMock.Object,
            routeHelper: reservedSlugTableMock.Object,
            localizationSettings: new LocalizationSettings(),
            seoSettings: _seoSettings,
            performanceSettings: new PerformanceSettings(),
            securitySettings: new SecuritySettings());

        PopulateEntities();
    }

    [TearDown]
    public void Reset()
    {
        RemoveEntities();
    }

    private void PopulateEntities()
    {
        int num = 50;
        var db = DbContext;

        for (var i = 1; i <= num; i++)
        {
            db.Topics.Add(new Topic { Title = $"Topic {i} Test" });
            _articles.Add(new NamedEntity { Id = _articles.Count + 1, EntityName = "Article", DisplayName = $"Article {i} Test" });
        }

        db.SaveChanges();
    }

    private void RemoveEntities()
    {
        var db = DbContext;
        db.Topics.RemoveRange(db.Topics.ToList());
        _articles.Clear();
        db.UrlRecords.RemoveRange(db.UrlRecords.ToList());

        db.SaveChanges();
    }

    #endregion

    [Test]
    public async Task CanDetectCollisions()
    {
        var db = DbContext;

        await PopulateSlugs(db.Topics.ToList());

        for (var i = 2; i <= 10; i++)
        {
            // Add another topic with existing name
            var topic = new Topic { Title = "Topic 1 Test" };
            db.Topics.Add(topic);
            await db.SaveChangesAsync();
            var validateSlugResult = await _urlService.ValidateSlugAsync(topic, seName: null, ensureNotEmpty: true);
            await _urlService.ApplySlugAsync(validateSlugResult);
            await db.SaveChangesAsync();

            var activeSlug = await _urlService.GetActiveSlugAsync(topic.Id, topic.GetEntityName(), 0);
            activeSlug.ShouldEqual($"topic-1-test-{i}");
        }
    }

    [Test]
    public async Task CanPopulateSlugs()
    {
        var db = DbContext;

        var topics = db.Topics.ToList();
        var articles = _articles.ToList();

        await PopulateSlugs(topics);
        await PopulateSlugs(articles);

        var slugs = db.UrlRecords.ToList();
        slugs.Count.ShouldEqual(topics.Count + articles.Count);

        var topic11 = topics.First(x => x.Title == "Topic 11 Test");
        var activeSlug = await _urlService.GetActiveSlugAsync(topic11.Id, topic11.GetEntityName(), 0);
        activeSlug.ShouldEqual("topic-11-test");

        var article22 = articles.First(x => x.DisplayName == "Article 22 Test");
        activeSlug = await _urlService.GetActiveSlugAsync(article22.Id, article22.GetEntityName(), 0);
        activeSlug.ShouldEqual("article-22-test");

        var collection = await _urlService.GetUrlRecordCollectionAsync(
            NamedEntity.GetEntityName<Topic>(),
            languageIds: null,
            entityIds: topics.Select(x => x.Id).Take(20).ToArray(),
            isRange: true,
            isSorted: true);

        collection.Count.ShouldEqual(20);

        var topic10 = topics.First(x => x.Title == "Topic 10 Test");
        var cachedSlug = collection.Find(0, topic10.Id);
        Assert.That(cachedSlug, Is.Not.Null);
        cachedSlug.Slug.ShouldEqual("topic-10-test");

        // Nor prefetch
        await _urlService.PrefetchUrlRecordsAsync(
            "Article",
            languageIds: null,
            entityIds: null);

        activeSlug = await _urlService.GetActiveSlugAsync(article22.Id, article22.GetEntityName(), 0);
        activeSlug.ShouldEqual("article-22-test");
    }

    [Test]
    public async Task CanPopulateSlugsBatched()
    {
        var db = DbContext;

        var topics = db.Topics.ToList();
        var articles = _articles.ToList();

        await PopulateSlugsBatched(topics.Cast<ISlugSupported>().Concat(articles));

        var slugs = db.UrlRecords.ToList();
        slugs.Count.ShouldEqual(topics.Count + articles.Count);

        // Populate new entities with identical names
        PopulateEntities();
        var newTopics = db.Topics.ToList().Except(topics).ToList();
        var newArticles = _articles.ToList().Except(articles).ToList();

        // Populate with collision
        await PopulateSlugsBatched(newTopics.Cast<ISlugSupported>().Concat(newArticles));

        slugs = db.UrlRecords.ToList();
        slugs.Count.ShouldEqual((newTopics.Count + newArticles.Count) * 2);

        // Populate same
        await PopulateSlugsBatched(newTopics.Cast<ISlugSupported>().Concat(newArticles));
        // Should do nothing as nothing changed
        slugs = db.UrlRecords.ToList();
        slugs.Count.ShouldEqual((newTopics.Count + newArticles.Count) * 2);
    }

    private async Task PopulateSlugs(IEnumerable<ISlugSupported> entities)
    {
        foreach (var entity in entities)
        {
            var validateSlugResult = await _urlService.ValidateSlugAsync(entity, seName: null, ensureNotEmpty: true);
            await _urlService.ApplySlugAsync(validateSlugResult);
        }

        await DbContext.SaveChangesAsync();
    }

    private async Task PopulateSlugsBatched(IEnumerable<ISlugSupported> entities)
    {
        using var scope = _urlService.CreateBatchScope();

        foreach (var entity in entities)
        {
            scope.ApplySlugs(new ValidateSlugResult
            {
                Source = entity,
                Slug = SlugUtility.Slugify(entity.GetDisplayName(), _seoSettings)
            });
        }

        await scope.CommitAsync();
    }
}
