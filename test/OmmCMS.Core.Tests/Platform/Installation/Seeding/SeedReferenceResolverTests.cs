using System.Collections.Generic;
using NUnit.Framework;
using OmmCMS.Core.Catalog.Categories;
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
        var category = new Category { Name = "Normal" };
        resolver.Add("standard", category);

        Assert.That(resolver.Get<Category>("STANDARD"), Is.SameAs(category));
        Assert.That(resolver.TryGet<Country>("standard", out _), Is.False);
    }

    [Test]
    public void Aliases_resolve_with_a_single_hop()
    {
        var resolver = new SeedReferenceResolver();
        var category = new Category { Name = "Normal" };
        resolver.Add("standard", category);
        resolver.AddAliases<Category>(new Dictionary<string, string> { ["apparel"] = "standard", ["shoes"] = "apparel" });

        Assert.That(resolver.Get<Category>("apparel"), Is.SameAs(category));
        Assert.That(resolver.TryGet<Category>("shoes", out _), Is.False);
    }

    [Test]
    public void Registered_keys_win_over_aliases()
    {
        var resolver = new SeedReferenceResolver();
        var apparel = new Category { Name = "Apparel" };
        resolver.Add("apparel", apparel);
        resolver.Add("standard", new Category { Name = "Normal" });
        resolver.AddAliases<Category>(new Dictionary<string, string> { ["apparel"] = "standard" });

        Assert.That(resolver.Get<Category>("apparel"), Is.SameAs(apparel));
    }

    [Test]
    public void Unknown_key_throws()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() => new SeedReferenceResolver().Get<Category>("missing"));
        Assert.That(ex.Message, Does.Contain("Category key 'missing'"));
    }

    [Test]
    public void Duplicate_key_throws()
    {
        var resolver = new SeedReferenceResolver();
        resolver.Add("standard", new Category());

        Assert.Throws<SeedDataFormatException>(() => resolver.Add("Standard", new Category()));
    }

    [Test]
    public void GetId_by_type_name_resolves_keys_and_aliases()
    {
        var resolver = new SeedReferenceResolver();
        resolver.Add("standard", new Category { Id = 4 });
        resolver.AddAliases<Category>(new Dictionary<string, string> { ["apparel"] = "standard" });

        Assert.That(resolver.GetId("taxcategory", "STANDARD"), Is.EqualTo(4));
        Assert.That(resolver.GetId("Category", "apparel"), Is.EqualTo(4));
        Assert.Throws<SeedDataFormatException>(() => resolver.GetId("Currency", "EUR"));
        Assert.Throws<SeedDataFormatException>(() => resolver.GetId("Category", "missing"));
    }

    [Test]
    public void GetId_requires_a_saved_entity()
    {
        var resolver = new SeedReferenceResolver();
        var category = new Category();
        resolver.Add("standard", category);

        Assert.Throws<SeedDataFormatException>(() => resolver.GetId<Category>("standard"));

        category.Id = 3;
        Assert.That(resolver.GetId<Category>("standard"), Is.EqualTo(3));
    }
}
