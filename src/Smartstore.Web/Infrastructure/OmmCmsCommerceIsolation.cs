using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Smartstore.Web.Infrastructure;

/// <summary>
/// Blocks known commerce MVC endpoints in CMS preview, independent of the URL or HTTP verb.
/// </summary>
public static class OmmCmsCommerceIsolation
{
    private static readonly HashSet<string> _publicControllers = new(StringComparer.Ordinal)
    {
        "CatalogController", "ProductController", "CheckoutController",
        "ShoppingCartController", "OrderController", "ReturnCaseController",
        "DownloadController", "PdfController", "SearchController"
    };

    private static readonly HashSet<string> _adminControllers = new(StringComparer.Ordinal)
    {
        "AffiliateController", "BarcodeTesterController", "CampaignController",
        "CategoryController", "CheckoutAttributeController", "CollectionGroupController",
        "DeliveryTimeController", "DiscountController", "GiftCardController",
        "ManufacturerController", "MeasureController", "OrderController",
        "ImportController", "ExportController", "CurrencyController",
        "PaymentController", "PriceLabelController",
        "ProductAttributeController", "ProductController", "ProductReviewController",
        "QuantityUnitController", "RecurringPaymentController", "ReturnCaseController",
        "SearchController", "ShipmentController", "ShippingController",
        "ShoppingCartController", "SpecificationAttributeController", "TaxController"
    };

    private static readonly HashSet<string> _publicCustomerActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Orders", "CancelRecurringPayment", "ReturnCases", "DownloadableProducts",
        "UserAgreement", "RewardPoints", "StockSubscriptions", "StockSubscriptionsPOST"
    };

    private static readonly HashSet<string> _adminCustomerActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "RemoveAffiliateAssignment", "UpdateVatNumberStatus", "RewardPointsHistoryList",
        "RewardPointsInsert", "Reports", "ReportTopCustomersList", "GetCartList",
        "RewardPointsSettings"
    };

    private static readonly HashSet<string> _adminDashboardActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "DashboardConfigure", "DashboardWidget", "DashboardSelect", "DashboardPreview", "DashboardSave"
    };

    private static readonly HashSet<string> _adminSettingActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "AllSettings", "SettingList", "SettingUpdate", "SettingInsert", "SettingDelete"
    };

    private static readonly HashSet<string> _commercePickerEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Product", "Category", "Manufacturer"
    };

    private static readonly HashSet<string> _patchEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Topic", "Menu", "MenuItem", "MediaFile", "MediaFolder", "Language", "Store",
        "Customer", "CustomerRole", "UrlRecord", "EmailAccount", "MessageTemplate"
    };

    /// <summary>
    /// Indicates whether an entity picker exposes commerce catalog data.
    /// </summary>
    public static bool IsCommercePickerEntity(string entityName)
        => _commercePickerEntities.Contains(entityName);

    /// <summary>
    /// Limits generic entity patch endpoints to reviewed CMS and platform entities.
    /// </summary>
    public static bool IsAllowedPatchEntity(string entityName)
        => _patchEntities.Contains(entityName);

    /// <summary>
    /// Classifies the selected MVC action, not user-supplied route values.
    /// </summary>
    public static bool IsBlocked(ControllerActionDescriptor action)
    {
        if (action?.ControllerTypeInfo == null)
        {
            return false;
        }

        var type = action.ControllerTypeInfo;
        var name = type.Name;
        var methodName = action.MethodInfo?.Name ?? action.ActionName;

        if (type.Namespace == "Smartstore.Web.Controllers")
        {
            return _publicControllers.Contains(name)
                || (name == "CustomerController" && IsOneOf(_publicCustomerActions))
                || (name == "CommonController" && IsAction("CurrencySelected"));
        }

        if (type.Namespace == "Smartstore.Admin.Controllers")
        {
            return _adminControllers.Contains(name)
                || (name == "CustomerController" && IsOneOf(_adminCustomerActions))
                || (name == "HomeController" && IsOneOf(_adminDashboardActions))
                || (name == "SettingController" && IsOneOf(_adminSettingActions));
        }

        // The upstream admin Packaging controller uses a different namespace.
        return type.FullName == "Smartstore.Controllers.PackagingController";

        bool IsOneOf(HashSet<string> actions)
            => actions.Contains(action.ActionName) || actions.Contains(methodName);

        bool IsAction(string value)
            => string.Equals(action.ActionName, value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(methodName, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Short-circuits blocked endpoints before authorization, model binding and action execution.
    /// Called after endpoint routing only when the CMS preview is enabled.
    /// </summary>
    public static Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (IsBlocked(action))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        return next(context);
    }
}
