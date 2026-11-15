using Microsoft.AspNetCore.Mvc;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Data;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Messaging.Events;
using OmmCMS.Core.Stores;
using OmmCMS.Events;
using OmmCMS.Utilities;
using OmmCMS.Utilities.Html;

namespace OmmCMS.Core.Messaging;

public partial class MessageModelHelper
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILocalizationService _localizationService;
    private readonly Lazy<IUrlHelper> _urlHelper;

    public MessageModelHelper(
        SmartDbContext db,
        IWorkContext workContext,
        IDateTimeHelper dateTimeHelper,
        IEventPublisher eventPublisher,
        ILocalizationService localizationService,
        Lazy<IUrlHelper> urlHelper)
    {
        _db = db;
        _workContext = workContext;
        _dateTimeHelper = dateTimeHelper;
        _eventPublisher = eventPublisher;
        _localizationService = localizationService;
        _urlHelper = urlHelper;
    }

    public void ApplyCustomerContentPart(IDictionary<string, object> model, CustomerContent content, MessageContext ctx)
    {
        model["CustomerId"] = content.CustomerId;
        model["IpAddress"] = content.IpAddress;
        model["CreatedOn"] = ToUserDate(content.CreatedOnUtc, ctx);
        model["UpdatedOn"] = ToUserDate(content.UpdatedOnUtc, ctx);
    }

    public string BuildUrl(string url, MessageContext ctx)
    {
        return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + url.EmptyNull().EnsureStartsWith('/');
    }

    public string BuildRouteUrl(object routeValues, MessageContext ctx)
    {
        return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Value?.RouteUrl(routeValues);
    }

    public string BuildRouteUrl(string routeName, object routeValues, MessageContext ctx)
    {
        return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Value?.RouteUrl(routeName, routeValues);
    }

    public string BuildActionUrl(string action, string controller, object routeValues, MessageContext ctx)
    {
        return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Value?.Action(action, controller, routeValues);
    }

    public Task PublishModelPartCreatedEventAsync<T>(T source, dynamic part) where T : class
    {
        return _eventPublisher.PublishAsync(new MessageModelPartCreatedEvent<T>(source, part));
    }

    public async Task<object> GetTopicAsync(string topicSystemName, MessageContext ctx)
    {
        var topic = await _db.Topics
            .AsNoTracking()
            .ApplyStoreFilter(ctx.StoreId ?? 0)
            .FirstOrDefaultAsync(x => x.SystemName == topicSystemName);

        string body = topic?.GetLocalized(x => x.Body, ctx.Language);
        if (body.HasValue())
        {
            body = HtmlUtility.RelativizeFontSizes(body);
        }

        return new
        {
            Title = topic?.GetLocalized(x => x.Title, ctx.Language).Value.NullEmpty(),
            Body = body.NullEmpty()
        };
    }

    public string GetBoolResource(bool value, MessageContext ctx)
    {
        return _localizationService.GetResource(value ? "Common.Yes" : "Common.No", ctx.Language.Id);
    }

    public DateTime? ToUserDate(DateTime? utcDate, MessageContext messageContext)
    {
        if (utcDate == null)
            return null;

        return _dateTimeHelper.ConvertToUserTime(
            utcDate.Value,
            TimeZoneInfo.Utc,
            _dateTimeHelper.GetCustomerTimeZone(messageContext.Customer));
    }

    public Money FormatPrice(decimal price, MessageContext messageContext, decimal exchangeRate = 1)
    {
        return FormatPrice(price, (Currency)null, messageContext, exchangeRate);
    }

    public Money FormatPrice(decimal price, string currencyCode, MessageContext messageContext, decimal exchangeRate = 1)
    {
        // Currencies are cached, so no need for async in this simple case.
        var currency = _db.Currencies
            .AsNoTracking()
            .Where(x => x.CurrencyCode == currencyCode)
            .FirstOrDefault();

        return FormatPrice(
            price,
            currency ?? new Currency { CurrencyCode = currencyCode },
            messageContext,
            exchangeRate);
    }

    public Money FormatPrice(decimal price, Currency currency, MessageContext messageContext, decimal exchangeRate = 1)
    {
        currency ??= _workContext.WorkingCurrency;

        if (exchangeRate != 1)
        {
            return new(price * exchangeRate, currency);
        }

        return new(price, currency);
    }

    public static string GetDisplayNameForCustomer(Customer customer)
    {
        var fullName =
            customer.GetFullName().NullEmpty() ??
            customer.Username ??
            customer.FindEmail();

        return fullName?
            .Replace(",", string.Empty)
            .Replace(";", string.Empty);
    }

    // INFO: parameters must be of type 'string'. Type 'object' outputs nothing.
    public static string[] GetValidValues(params string[] values)
        => values.Where(CommonHelper.IsTruthy).ToArray();
}
