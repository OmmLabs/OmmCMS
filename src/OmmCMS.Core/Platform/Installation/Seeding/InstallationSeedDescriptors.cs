using System.Text.Json.Nodes;
using OmmCMS.Core.Common;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Logging;
using OmmCMS.Core.Messaging;
using OmmCMS.Core.Stores;
using OmmCMS.Scheduling;

namespace OmmCMS.Core.Installation.Seeding;

/// <summary>
/// Seed descriptors of the installation data in "App_Data/Seed". See the README there for the file format.
/// </summary>
internal static class InstallationSeedDescriptors
{
    public static SeedEntityDescriptor<MeasureDimension> MeasureDimensions()
        => new SeedEntityDescriptor<MeasureDimension>("systemKeyword").FromFiles("core/measure-dimensions.json");

    public static SeedEntityDescriptor<MeasureWeight> MeasureWeights()
        => new SeedEntityDescriptor<MeasureWeight>("systemKeyword").FromFiles("core/measure-weights.json");

    public static SeedEntityDescriptor<Currency> Currencies()
        => new SeedEntityDescriptor<Currency>("currencyCode").FromFiles("core/currencies.json");

    public static SeedEntityDescriptor<DeliveryTime> DeliveryTimes()
        => new SeedEntityDescriptor<DeliveryTime>("key").FromFiles("core/delivery-times.json");

    public static SeedEntityDescriptor<QuantityUnit> QuantityUnits()
        => new SeedEntityDescriptor<QuantityUnit>("key").FromFiles("core/quantity-units.json");

    public static SeedEntityDescriptor<CustomerRole> CustomerRoles()
        => new SeedEntityDescriptor<CustomerRole>("systemName").FromFiles("core/customer-roles.json");

    public static SeedEntityDescriptor<EmailAccount> EmailAccounts()
        => new SeedEntityDescriptor<EmailAccount>("key").FromFiles("core/email-accounts.json");

    public static SeedEntityDescriptor<ActivityLogType> ActivityLogTypes()
        => new SeedEntityDescriptor<ActivityLogType>("systemKeyword").FromFiles("core/activity-log-types.json");

    public static SeedEntityDescriptor<TaskDescriptor> TaskDescriptors()
        => new SeedEntityDescriptor<TaskDescriptor>("type").FromFiles("core/tasks.json");

    /// <summary>
    /// Countries with their states. States can be referenced as "{countryKey}/{stateKey}", e.g. "DE/BW".
    /// </summary>
    public static SeedEntityDescriptor<Country> Countries()
        => new SeedEntityDescriptor<Country>("twoLetterIsoCode")
            .FromFiles("core/countries.json")
            .Child("stateProvinces", x => x.StateProvinces, new SeedEntityDescriptor<StateProvince>("abbreviation"));

    /// <summary>
    /// Topics. The seed-only field "slug" overrides the slug derived from the system name and is collected in <paramref name="slugs"/>.
    /// </summary>
    /// <param name="slugs">Receives slug overrides. Must compare by reference (<see cref="ReferenceEqualityComparer"/>).</param>
    public static SeedEntityDescriptor<Topic> Topics(IDictionary<Topic, string> slugs)
    {
        Guard.NotNull(slugs);

        return new SeedEntityDescriptor<Topic>("systemName")
            .FromFiles("core/cms/topics.json")
            .Bind("slug", (topic, node, _) => slugs[topic] = node.GetValue<string>());
    }

    /// <summary>
    /// The default store. URL and SSL are not seed data; they are taken from the current request.
    /// </summary>
    /// <param name="media">Creates a saved media file for a media reference.</param>
    public static SeedEntityDescriptor<Store> Stores(Func<JsonNode, MediaFile> media)
    {
        Guard.NotNull(media);

        return new SeedEntityDescriptor<Store>("key")
            .FromFiles("core/cms/stores.json")
            .Reference<Currency>("defaultCurrency", (store, currency) => store.DefaultCurrencyId = currency.Id)
            .Reference<Currency>("primaryExchangeRateCurrency", (store, currency) => store.PrimaryExchangeRateCurrencyId = currency.Id)
            .Bind("logoMediaFile", (store, node, _) => store.LogoMediaFileId = media(node).Id);
    }

    /// <summary>
    /// Built-in system customers (search engine, background task, PDF converter, webhook client).
    /// </summary>
    public static SeedEntityDescriptor<Customer> SystemCustomers()
        => new SeedEntityDescriptor<Customer>("systemName")
            .FromFiles("core/system-customers.json")
            .OnCreated((customer, _) =>
            {
                customer.CustomerGuid = Guid.NewGuid();
                customer.CreatedOnUtc = DateTime.UtcNow;
                customer.LastActivityDateUtc = DateTime.UtcNow;
            });

    /// <summary>
    /// Address of the admin user.
    /// </summary>
    public static SeedEntityDescriptor<Address> AdminAddress()
        => new SeedEntityDescriptor<Address>("key")
            .FromFiles("core/admin-address.json")
            .Reference<Country>("country", (address, country) => address.Country = country)
            .Reference<StateProvince>("stateProvince", (address, state) => address.StateProvince = state)
            .OnCreated((address, _) => address.CreatedOnUtc = DateTime.UtcNow);

    /// <summary>
    /// CMS menus with their items. Titles may reference locale resources.
    /// </summary>
    public static SeedEntityDescriptor<MenuEntity> Menus()
        => new SeedEntityDescriptor<MenuEntity>("systemName")
            .FromFiles("core/cms/menus.json")
            .Child("items", x => x.Items, new SeedEntityDescriptor<MenuItemEntity>("key"));

    /// <summary>
    /// Parses a media reference: a path below the media folder, or { "file": path, "name": seoName }.
    /// </summary>
    public static (string Path, string Name) ParseMediaReference(JsonNode node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var path) && path.HasValue())
        {
            return (path, null);
        }

        if (node is JsonObject obj && obj["file"] is JsonValue file && file.TryGetValue<string>(out path) && path.HasValue())
        {
            return (path, obj["name"]?.GetValue<string>());
        }

        throw new SeedDataFormatException("A media reference must be a path or an object with \"file\" and optional \"name\".");
    }

}
