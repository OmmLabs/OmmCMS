using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using OmmCMS.Core.Content.Media;
using OmmCMS.Domain;
using System;
using System.Collections.Generic;
using OmmCMS.Core.Common;
using OmmCMS.Core.Installation.Seeding;

namespace OmmCMS.Core.Tests.Platform.Installation.Seeding;

[TestFixture]
public class SeedEntityFactoryTests
{
    private SeedReferenceResolver _resolver;
    private SeedEntityFactory _factory;

    public enum SeedPageType { Simple, Grouped = 10 }

    public class SeedPage : BaseEntity
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public SeedPageType PageType { get; set; }
        public int? CountryId { get; set; }
        public DateTime? AvailableStartDateTimeUtc { get; set; }
        public DateTime? AvailableEndDateTimeUtc { get; set; }
        public List<SeedPageTag> Tags { get; set; } = new();
    }

    public class SeedPageTag : BaseEntity
    {
        public MediaTag MediaTag { get; set; }
        public int DisplayOrder { get; set; }
    }

    [SetUp]
    public void SetUp()
    {
        _resolver = new SeedReferenceResolver();
        _factory = new SeedEntityFactory(_resolver);
    }

    private static SeedRecord Record(string key, string json)
        => new(key, JsonNode.Parse(json).AsObject(), "base/test.json");

    private static SeedEntityDescriptor<Country> CountryDescriptor()
        => new SeedEntityDescriptor<Country>("twoLetterIsoCode")
            .Child("stateProvinces", x => x.StateProvinces, new SeedEntityDescriptor<StateProvince>("abbreviation"));

    private static SeedEntityDescriptor<SeedPage> PageDescriptor()
        => new SeedEntityDescriptor<SeedPage>("key")
            .Reference<Country>("country", (p, t) => p.CountryId = t.Id)
            .References<MediaTag>("tags", (p, tags) =>
            {
                for (var i = 0; i < tags.Count; i++)
                {
                    p.Tags.Add(new SeedPageTag { MediaTag = tags[i], DisplayOrder = i + 1 });
                }
            });

    [Test]
    public void Deserializes_scalar_fields_and_registers_entity()
    {
        var record = Record("DE", """{ "twoLetterIsoCode": "DE", "name": "Deutschland", "numericIsoCode": 276, "published": true, "diallingCode": 49 }""");

        var country = _factory.Create(record, CountryDescriptor());

        Assert.That(country.Name, Is.EqualTo("Deutschland"));
        Assert.That(country.NumericIsoCode, Is.EqualTo(276));
        Assert.That(country.DiallingCode, Is.EqualTo(49));
        Assert.That(country.Published, Is.True);
        Assert.That(_resolver.Get<Country>("de"), Is.SameAs(country));
    }

    [Test]
    public void Enums_are_read_by_name()
    {
        var page = _factory.Create(Record("P-1", """{ "key": "P-1", "pageType": "Grouped" }"""), new SeedEntityDescriptor<SeedPage>("key"));

        Assert.That(page.PageType, Is.EqualTo(SeedPageType.Grouped));
    }

    [Test]
    public void Enum_integers_are_rejected()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "key": "P-1", "pageType": 10 }"""), new SeedEntityDescriptor<SeedPage>("key")));

    [Test]
    public void Unknown_field_throws_with_context()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "nmae": "Typo" }"""), CountryDescriptor()));

        Assert.That(ex.Message, Does.Contain("base/test.json"));
        Assert.That(ex.Message, Does.Contain("Country 'DE'"));
        Assert.That(ex.Message, Does.Contain("nmae"));
    }

    [Test]
    public void Id_field_is_rejected()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "id": 5 }"""), CountryDescriptor()));

    [Test]
    public void Navigation_fields_are_rejected_unless_bound()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "key": "P-1", "tags": [] }"""), new SeedEntityDescriptor<SeedPage>("key")));

    [Test]
    public void Child_collections_are_created_and_registered_with_composite_key()
    {
        var record = Record("DE", """
            {
              "twoLetterIsoCode": "DE",
              "name": "Deutschland",
              "stateProvinces": [ { "abbreviation": "BW", "name": "Baden-Württemberg" }, { "abbreviation": "BY", "name": "Bayern" } ]
            }
            """);

        var country = _factory.Create(record, CountryDescriptor());

        Assert.That(country.StateProvinces.Select(x => x.Name), Is.EquivalentTo(new[] { "Baden-Württemberg", "Bayern" }));
        Assert.That(_resolver.Get<StateProvince>("DE/BY").Name, Is.EqualTo("Bayern"));
    }

    [Test]
    public void Invalid_child_field_reports_the_child_path()
    {
        var record = Record("DE", """{ "twoLetterIsoCode": "DE", "stateProvinces": [ { "abbreviation": "BW", "foo": 1 } ] }""");

        var ex = Assert.Throws<SeedDataFormatException>(() => _factory.Create(record, CountryDescriptor()));
        Assert.That(ex.Message, Does.Contain("StateProvince 'DE/BW'"));
    }

    [Test]
    public void References_are_resolved_including_aliases()
    {
        _resolver.Add("standard", new Country { Id = 7, Name = "Normal" });
        _resolver.AddAliases<Country>([new("apparel", "standard")]);

        var golf = new MediaTag { Name = "Golf" };
        var sports = new MediaTag { Name = "Sports" };
        _resolver.Add("golf", golf);
        _resolver.Add("sports", sports);

        var page = _factory.Create(Record("P-1", """{ "key": "P-1", "country": "apparel", "tags": [ "sports", "GOLF" ] }"""), PageDescriptor());

        Assert.That(page.CountryId, Is.EqualTo(7));
        Assert.That(page.Tags.OrderBy(x => x.DisplayOrder).Select(x => x.MediaTag), Is.EqualTo(new[] { sports, golf }));
    }

    [Test]
    public void Unknown_reference_throws_with_field()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "key": "P-1", "country": "missing" }"""), PageDescriptor()));

        Assert.That(ex.Message, Does.Contain("country"));
        Assert.That(ex.Message, Does.Contain("missing"));
    }

    [Test]
    public void Null_bound_fields_are_skipped()
    {
        var page = _factory.Create(Record("P-1", """{ "key": "P-1", "country": null }"""), PageDescriptor());

        Assert.That(page.CountryId, Is.Null);
    }

    [Test]
    public void Custom_binder_receives_value_and_context()
    {
        string boundKey = null;
        var descriptor = new SeedEntityDescriptor<SeedPage>("key")
            .Bind("pictures", (p, node, context) =>
            {
                boundKey = context.Key;
                p.Name = string.Join(",", node.AsArray().Select(x => (string)x));
            });

        var page = _factory.Create(Record("P-1", """{ "key": "P-1", "pictures": [ "a.jpg", "b.jpg" ] }"""), descriptor);

        Assert.That(page.Name, Is.EqualTo("a.jpg,b.jpg"));
        Assert.That(boundKey, Is.EqualTo("P-1"));
    }

    [Test]
    public void Resource_references_are_resolved_in_fields_and_children()
    {
        var resources = new SeedResources("de-DE", new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Countries.Germany"] = "Deutschland",
            ["States.BW"] = "Baden-Württemberg"
        });

        var factory = new SeedEntityFactory(_resolver, resources);
        var record = Record("DE", """{ "twoLetterIsoCode": "DE", "name": "@resource:countries.germany", "stateProvinces": [ { "abbreviation": "BW", "name": "@resource:States.BW" } ] }""");

        var country = factory.Create(record, CountryDescriptor());

        Assert.That(country.Name, Is.EqualTo("Deutschland"));
        Assert.That(country.StateProvinces.Single().Name, Is.EqualTo("Baden-Württemberg"));
    }

    [Test]
    public void Embedded_resource_references_are_resolved()
    {
        var resources = new SeedResources("de-DE", new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Footer.Info"] = "Informationen",
            ["Footer.Service"] = "Service"
        });

        var factory = new SeedEntityFactory(_resolver, resources);
        var country = factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "name": "Footer - {@resource:Footer.Info} / {@resource:footer.service}" }"""), CountryDescriptor());

        Assert.That(country.Name, Is.EqualTo("Footer - Informationen / Service"));
    }

    [Test]
    public void Resource_reference_without_seeded_resources_throws()
    {
        var ex = Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "name": "@resource:Countries.Germany" }"""), CountryDescriptor()));

        Assert.That(ex.Message, Does.Contain("no locale resources have been seeded"));
    }

    [Test]
    public void Unknown_resource_reference_throws()
    {
        var factory = new SeedEntityFactory(_resolver, new SeedResources("de-DE", new System.Collections.Generic.Dictionary<string, string>()));

        var ex = Assert.Throws<SeedDataFormatException>(() =>
            factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "name": "@resource:Missing.Key" }"""), CountryDescriptor()));

        Assert.That(ex.Message, Does.Contain("unknown locale resource 'Missing.Key'"));
    }

    [Test]
    public void OnCreated_runs_after_binding_in_registration_order()
    {
        var calls = new System.Collections.Generic.List<string>();
        var descriptor = new SeedEntityDescriptor<SeedPage>("key")
            .Bind("pictures", (p, _, _) => calls.Add("bind"))
            .OnCreated((p, context) => calls.Add("created:" + context.Key + ":" + p.Name))
            .OnCreated((p, _) => calls.Add("created2"));

        _factory.Create(Record("P-1", """{ "key": "P-1", "name": "Ball", "pictures": [] }"""), descriptor);

        Assert.That(calls, Is.EqualTo(new[] { "bind", "created:P-1:Ball", "created2" }));
    }

    [Test]
    public void Relative_dates_are_resolved()
    {
        var before = System.DateTime.UtcNow;
        var page = _factory.Create(Record("P-1", """{ "key": "P-1", "availableStartDateTimeUtc": "@now-10d", "availableEndDateTimeUtc": "@now+1M" }"""), new SeedEntityDescriptor<SeedPage>("key"));

        Assert.That(page.AvailableStartDateTimeUtc, Is.EqualTo(before.AddDays(-10)).Within(System.TimeSpan.FromMinutes(1)));
        Assert.That(page.AvailableEndDateTimeUtc, Is.EqualTo(before.AddMonths(1)).Within(System.TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void Invalid_relative_date_throws()
        => Assert.Throws<SeedDataFormatException>(() =>
            _factory.Create(Record("P-1", """{ "key": "P-1", "availableEndDateTimeUtc": "@now+1y" }"""), new SeedEntityDescriptor<SeedPage>("key")));

    [Test]
    public void Key_field_can_be_a_bound_reference()
    {
        _resolver.Add("standard", new Country { Id = 9 });
        var descriptor = new SeedEntityDescriptor<SeedPage>("country")
            .Reference<Country>("country", (p, t) => p.CountryId = t.Id);

        var page = _factory.Create(Record("standard", """{ "country": "standard", "key": "P-1" }"""), descriptor);

        Assert.That(page.CountryId, Is.EqualTo(9));
    }

    [Test]
    public void Child_list_allows_duplicates_and_is_not_registered()
    {
        var descriptor = new SeedEntityDescriptor<Country>("twoLetterIsoCode")
            .ChildList("stateProvinces", x => x.StateProvinces, new SeedEntityDescriptor<StateProvince>("abbreviation"));

        var country = _factory.Create(Record("DE", """{ "twoLetterIsoCode": "DE", "stateProvinces": [ { "abbreviation": "BW", "displayOrder": 1 }, { "abbreviation": "BW", "displayOrder": 5 } ] }"""), descriptor);

        Assert.That(country.StateProvinces.Select(x => x.DisplayOrder), Is.EquivalentTo(new[] { 1, 5 }));
        Assert.That(_resolver.TryGet<StateProvince>("DE/BW", out _), Is.False);
    }

    [Test]
    public void Binding_a_field_twice_throws()
        => Assert.Throws<System.InvalidOperationException>(() =>
            new SeedEntityDescriptor<SeedPage>("key").Bind("x", (_, _, _) => { }).Bind("X", (_, _, _) => { }));

    [Test]
    public void CreateAll_strips_synthetic_key_and_registers_aliases()
    {
        var descriptor = new SeedEntityDescriptor<MediaTag>("key");
        var set = new SeedRecordSet(
            descriptor,
            [Record("standard", """{ "key": "standard", "name": "Normal" }""")],
            new System.Collections.Generic.Dictionary<string, string> { ["apparel"] = "standard" });

        var tags = _factory.CreateAll<MediaTag>(set);

        Assert.That(tags.Single().Name, Is.EqualTo("Normal"));
        Assert.That(_resolver.Get<MediaTag>("apparel"), Is.SameAs(tags[0]));
    }

    [Test]
    public void CreateAll_rejects_record_sets_of_other_types()
    {
        var set = new SeedRecordSet(new SeedEntityDescriptor<MediaTag>("key"), [], new System.Collections.Generic.Dictionary<string, string>());

        Assert.Throws<System.ArgumentException>(() => _factory.CreateAll<Country>(set));
    }
}
