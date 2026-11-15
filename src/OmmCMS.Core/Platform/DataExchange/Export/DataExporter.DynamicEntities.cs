using System.Reflection;
using OmmCMS.ComponentModel;
using OmmCMS.Core.Common;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Content.Media.Imaging;
using OmmCMS.Core.DataExchange.Export.Events;
using OmmCMS.Core.DataExchange.Export.Internal;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Messaging;
using OmmCMS.Core.Stores;

namespace OmmCMS.Core.DataExchange.Export;

public partial class DataExporter
{
    private async Task<IEnumerable<dynamic>> Convert(Customer customer, DataExporterContext ctx)
    {
        var result = new List<dynamic>();
        var genericAttributes = await ctx.CustomerBatchContext.GenericAttributes.GetOrLoadAsync(customer.Id);

        dynamic dynObject = ToDynamic(customer);
        dynObject.BillingAddress = ToDynamic(customer.BillingAddress, ctx);
        dynObject.ShippingAddress = ToDynamic(customer.ShippingAddress, ctx);

        dynObject.Addresses = customer.Addresses
            .Select(x => ToDynamic(x, ctx))
            .ToList();

        dynObject._GenericAttributes = genericAttributes
            .Select(x => CreateDynamic(x))
            .ToList();

        dynObject._HasNewsletterSubscription = ctx.NewsletterSubscriptions.Contains(customer.Email, StringComparer.CurrentCultureIgnoreCase);
        dynObject._FullName = customer.GetFullName();
        dynObject._AvatarPictureUrl = null;

        if (_customerSettings.AllowCustomersToUploadAvatars)
        {
            // Reduce traffic and do not export default avatar.
            var fileId = genericAttributes.FirstOrDefault(x => x.Key == SystemCustomerAttributeNames.AvatarPictureId)?.Value?.ToInt() ?? 0;
            var file = await _mediaService.GetFileByIdAsync(fileId, MediaLoadFlags.AsNoTracking);
            if (file != null)
            {
                dynObject._AvatarPictureUrl = _mediaService.GetUrl(file, new ProcessImageQuery { MaxSize = _mediaSettings.AvatarPictureSize }, ctx.Store.GetBaseUrl());
            }
        }

        result.Add(dynObject);

        await _services.EventPublisher.PublishAsync(new RowExportingEvent
        {
            Row = dynObject,
            EntityType = ExportEntityType.Customer,
            ExportRequest = ctx.Request,
            ExecuteContext = ctx.ExecuteContext
        });

        return result;
    }

    private async Task<IEnumerable<dynamic>> Convert(NewsletterSubscription subscription, DataExporterContext ctx)
    {
        var result = new List<dynamic>();
        dynamic dynObject = DataExporter.ToDynamic(subscription, ctx);
        result.Add(dynObject);

        await _services.EventPublisher.PublishAsync(new RowExportingEvent
        {
            Row = dynObject,
            EntityType = ExportEntityType.NewsletterSubscription,
            ExportRequest = ctx.Request,
            ExecuteContext = ctx.ExecuteContext
        });

        return result;
    }


    private static dynamic ToDynamic(Currency currency, DataExporterContext ctx)
    {
        if (currency == null)
        {
            return null;
        }

        dynamic result = new DynamicEntity(currency);

        result.Name = ctx.GetTranslation(currency, nameof(currency.Name), currency.Name);
        result._Localized = GetLocalized(ctx, currency, x => x.Name);

        return result;
    }

    private static dynamic ToDynamic(Country country, DataExporterContext ctx)
    {
        if (country == null)
        {
            return null;
        }

        dynamic result = new DynamicEntity(country);

        result.Name = ctx.GetTranslation(country, nameof(country.Name), country.Name);
        result._Localized = GetLocalized(ctx, country, x => x.Name);

        return result;
    }

    private static dynamic ToDynamic(Address address, DataExporterContext ctx)
    {
        if (address == null)
        {
            return null;
        }

        var country = address.CountryId.HasValue
            ? ctx.Countries.Get(address.CountryId.Value)
            : null;

        var stateProvince = address.StateProvinceId.HasValue
            ? ctx.StateProvinces.Get(address.StateProvinceId.Value)
            : null;

        dynamic result = new DynamicEntity(address);

        result.Country = ToDynamic(country, ctx);

        if (stateProvince != null)
        {
            dynamic sp = new DynamicEntity(stateProvince);

            sp.Name = ctx.GetTranslation(stateProvince, nameof(stateProvince.Name), stateProvince.Name);
            sp._Localized = GetLocalized(ctx, stateProvince, x => x.Name);

            result.StateProvince = sp;
        }
        else
        {
            result.StateProvince = null;
        }

        return result;
    }

    private static dynamic ToDynamic(Customer customer)
    {
        if (customer == null)
        {
            return null;
        }

        dynamic result = new DynamicEntity(customer);

        result.BillingAddress = null;
        result.ShippingAddress = null;
        result.Addresses = null;


        result._GenericAttributes = null;
        result._HasNewsletterSubscription = false;

        result._FullName = null;
        result._AvatarPictureUrl = null;

        result.CustomerRoles = customer.CustomerRoleMappings
            .Select(x =>
            {
                dynamic dyn = new DynamicEntity(x.CustomerRole);
                return dyn;
            })
            .ToList();

        return result;
    }

    private static dynamic ToDynamic(Store store)
    {
        if (store == null)
        {
            return null;
        }

        dynamic result = new DynamicEntity(store);
        return result;
    }

    private static dynamic ToDynamic(NewsletterSubscription subscription, DataExporterContext ctx)
    {
        if (subscription == null)
        {
            return null;
        }

        dynamic result = new DynamicEntity(subscription);

        result.Store = ctx.Stores.ContainsKey(subscription.StoreId)
            ? DataExporter.ToDynamic(ctx.Stores[subscription.StoreId])
            : null;

        return result;
    }


    private static dynamic CreateDynamic<TEntity>(TEntity entity)
        where TEntity : BaseEntity
    {
        if (entity == null)
        {
            return null;
        }

        dynamic result = new DynamicEntity(entity);
        return result;
    }

    private static List<dynamic> GetLocalized<TEntity>(
        DataExporterContext ctx,
        TEntity entity,
        params Expression<Func<TEntity, string>>[] keySelectors)
        where TEntity : BaseEntity, ILocalizedEntity
    {
        if (ctx.Languages.Count <= 1)
        {
            return null;
        }

        var translations = ctx.GetTranslations<TEntity>();
        if (translations == null)
        {
            return null;
        }

        var slugs = ctx.GetUrlRecords<TEntity>();
        var result = new List<dynamic>();

        foreach (var language in ctx.Languages)
        {
            var languageCulture = language.Value.LanguageCulture.EmptyNull().ToLower();

            // Add slug.
            if (slugs != null)
            {
                var value = slugs.GetSlug(language.Value.Id, entity.Id, false);
                if (value.HasValue())
                {
                    dynamic exp = new HybridExpando();
                    exp.Culture = languageCulture;
                    exp.LocaleKey = "SeName";
                    exp.LocaleValue = value;

                    result.Add(exp);
                }
            }

            // Add localized property value.
            foreach (var keySelector in keySelectors)
            {
                var member = keySelector.Body as MemberExpression;
                var propInfo = member.Member as PropertyInfo;
                string localeKey = propInfo.Name;
                var value = translations.GetValue(language.Value.Id, entity.Id, localeKey);

                // We do not export empty values to not fill databases with it.
                if (value.HasValue())
                {
                    dynamic exp = new HybridExpando();
                    exp.Culture = languageCulture;
                    exp.LocaleKey = localeKey;
                    exp.LocaleValue = value;

                    result.Add(exp);
                }
            }
        }

        return result.Any() ? result : null;
    }
}
