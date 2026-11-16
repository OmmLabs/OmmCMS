using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OmmCMS.Core.Data;
using OmmCMS.Core.Installation.Seeding;
using OmmCMS.IO;

namespace OmmCMS.Core.Tests.Platform.Installation;

/// <summary>
/// Runs the real installation seeder with the real seed files for every installable culture.
/// The loader fails on unknown fields, unknown keys, broken references and missing files, so a successful
/// run already proves a lot. The tests require CMS-only starter content for any supported culture.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Seeding")]
public partial class InstallationSeedingTests
{
    private static IEnumerable<TestCaseData> Cultures()
        => SeedDataSource.DiscoverCultures(new LocalFileSystem(SeedingHarness.SeedSourceDir))
            .Select(x => new TestCaseData(x).SetArgDisplayNames(x.Culture));

    [TestCaseSource(nameof(Cultures))]
    public async Task Mandatory_data_is_seeded(SeedCulture culture)
    {
        using var harness = await SeedingHarness.SeedAsync(culture, seedSampleData: false);
        var db = harness.Db;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(db.Stores.Any(), Is.True, "Stores");
            Assert.That(db.Currencies.Any(), Is.True, "Currencies");
            Assert.That(db.Countries.Any(), Is.True, "Countries");
            Assert.That(db.Settings.Any(), Is.True, "Settings");
            Assert.That(db.Model.GetEntityTypes().Any(x => x.ClrType.Name == "Product"), Is.False, "Product persistence must be absent.");
        }
    }

    [TestCaseSource(nameof(Cultures))]
    public async Task Legacy_sample_request_still_seeds_only_cms_content(SeedCulture culture)
    {
        using var harness = await SeedingHarness.SeedAsync(culture, seedSampleData: true);
        var db = harness.Db;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(db.Model.GetEntityTypes().Any(x => x.ClrType.Name == "Product"), Is.False, "Product persistence must be absent.");
            Assert.That(db.Model.GetEntityTypes().Any(x => x.ClrType.Name == "Category"), Is.False, "Category persistence must be absent.");
            Assert.That(db.Set<OmmCMS.Core.Content.Topics.Topic>().Any(x => x.SystemName == "HomePageText"), Is.True);
        }
    }

    [TestCaseSource(nameof(Cultures))]
    public async Task Cms_mode_seeds_content_without_commerce(SeedCulture culture)
    {
        using var harness = await SeedingHarness.SeedAsync(culture, seedSampleData: true);
        var db = harness.Db;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(db.Stores.Select(x => x.Name).Single(), Is.EqualTo("OmmCMS"));
            Assert.That(db.Model.GetEntityTypes().Any(x => x.ClrType.Name == "Product"), Is.False, "Product persistence must be absent.");
            Assert.That(db.Model.GetEntityTypes().Any(x => x.ClrType.Name == "Category"), Is.False, "Category persistence must be absent.");
            var topics = db.Set<OmmCMS.Core.Content.Topics.Topic>().Select(x => x.SystemName).ToList();
            Assert.That(topics, Does.Contain("HomePageText"));
            Assert.That(topics, Does.Contain("AboutUs"));
            Assert.That(topics, Does.Not.Contain("ShippingInfo"));
            Assert.That(topics, Does.Not.Contain("PaymentInfo"));
            Assert.That(topics, Does.Not.Contain("CheckoutAsGuestOrRegister"));
            Assert.That(db.Set<OmmCMS.Core.Content.Menus.MenuItemEntity>().Any(x => x.ProviderName == "catalog"), Is.False);
            var tasks = db.Set<OmmCMS.Scheduling.TaskDescriptor>().Select(x => x.Type).ToList();
            Assert.That(tasks, Is.EquivalentTo(new[]
            {
                "QueuedMessagesSendTask", "QueuedMessagesClearTask", "DeleteGuestsTask", "DeleteLogsTask",
                "ClearCacheTask", "TransientMediaClearTask", "TempFileCleanupTask", "RebuildXmlSitemapTask",
                "TargetGroupEvaluatorTask"
            }));
            Assert.That(tasks, Does.Not.Contain("ProductRuleEvaluatorTask"));
            Assert.That(tasks, Does.Not.Contain("UpdateExchangeRateTask"));
        }
    }
}
