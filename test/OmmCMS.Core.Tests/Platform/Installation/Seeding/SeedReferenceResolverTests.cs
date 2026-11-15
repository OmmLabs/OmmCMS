using System.Collections.Generic;
using NUnit.Framework;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Common;
using OmmCMS.Core.Installation.Seeding;

namespace OmmCMS.Core.Tests.Platform.Installation.Seeding;

[TestFixture]
public class SeedReferenceResolverTests
{
    [Test]
    public void Keys_are_case_insensitive_and_scoped_per_type()
    {
        var resolver = new SeedReferenceResolver();
        var category = new MediaTag { Name = "Normal" };
        resolver.Add("standard", category);

        Assert.That(resolver.Get<MediaTag>("STANDARD"), Is.SameAs(category));
        Assert.That(resolver.TryGet<Country>("standard", out _), Is.False);
    }

    [Test]
    public void Aliases_resolve_with_a_single_hop()
    {
        var resolver = new SeedReferenceResolver();
        var category = new MediaTag { Name = "Normal" };
        resolver.Add("standard", category);
        resolver.AddAliases<MediaTag>(new Dictionary<string, string> { ["apparel"] = "standard", ["shoes"] = "apparel" });

        Assert.That(resolver.Get<MediaTag>("apparel"), Is.SameAs(category));
        Assert.That(resolver.TryGet<MediaTag>("shoes", out _), Is.False);
    }

    [Test]
    public void Registered_keys_win_over_aliases()
    {
        var resolver = new SeedReferenceResolver();
        var apparel = new MediaTag { Name = "Apparel" };
        resolver.Add("apparel", apparel);
        resolver.Add("standard", new MediaTag { Name = "Normal" });
        resolver.AddAliases<MediaTag>(new Dictionary<string, string> { ["apparel"] = "standard" });

        Assert.That(resolver.Get<MediaTag>("apparel"), Is.SameAs(apparel));
    }

    [Test]
    public void Unknown_key_throws()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() => new SeedReferenceResolver().Get<MediaTag>("missing"));
        Assert.That(ex.Message, Does.Contain("MediaTag key 'missing'"));
    }

    [Test]
    public void Duplicate_key_throws()
    {
        var resolver = new SeedReferenceResolver();
        resolver.Add("standard", new MediaTag());

        Assert.Throws<SeedDataFormatException>(() => resolver.Add("Standard", new MediaTag()));
    }

    [Test]
    public void GetId_by_type_name_resolves_keys_and_aliases()
    {
        var resolver = new SeedReferenceResolver();
        resolver.Add("standard", new MediaTag { Id = 4 });
        resolver.AddAliases<MediaTag>(new Dictionary<string, string> { ["apparel"] = "standard" });

        Assert.That(resolver.GetId("taxcategory", "STANDARD"), Is.EqualTo(4));
        Assert.That(resolver.GetId("MediaTag", "apparel"), Is.EqualTo(4));
        Assert.Throws<SeedDataFormatException>(() => resolver.GetId("Currency", "EUR"));
        Assert.Throws<SeedDataFormatException>(() => resolver.GetId("MediaTag", "missing"));
    }

    [Test]
    public void GetId_requires_a_saved_entity()
    {
        var resolver = new SeedReferenceResolver();
        var category = new MediaTag();
        resolver.Add("standard", category);

        Assert.Throws<SeedDataFormatException>(() => resolver.GetId<MediaTag>("standard"));

        category.Id = 3;
        Assert.That(resolver.GetId<MediaTag>("standard"), Is.EqualTo(3));
    }
}
