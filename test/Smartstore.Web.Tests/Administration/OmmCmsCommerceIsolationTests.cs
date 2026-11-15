using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Smartstore.Engine;
using Smartstore.Web.Infrastructure;

namespace Smartstore.Web.Tests.Administration;

[TestFixture]
public class OmmCmsCommerceIsolationTests
{
    private static ControllerActionDescriptor Action(string controllerType, string methodName = null, string alias = null)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(controllerType);
        Assert.That(type, Is.Not.Null, $"The actual production controller {controllerType} must exist.");

        var method = methodName == null
            ? null
            : type!.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.Name == methodName);

        if (methodName != null)
        {
            Assert.That(method, Is.Not.Null, $"The production method {methodName} must exist.");
        }

        return new ControllerActionDescriptor
        {
            ControllerTypeInfo = type!.GetTypeInfo(),
            ControllerName = type.Name.Replace("Controller", ""),
            MethodInfo = method,
            ActionName = alias ?? methodName ?? "Index"
        };
    }

    [TestCase("Smartstore.Web.Controllers.PdfController")]
    [TestCase("Smartstore.Controllers.PackagingController")]
    public void BlocksEveryActionOnCommerceOnlyController(string controllerType)
    {
        Assert.That(OmmCmsCommerceIsolation.IsBlocked(Action(controllerType)), Is.True);
    }

    [TestCase("Smartstore.Admin.Controllers.AffiliateController")]
    [TestCase("Smartstore.Admin.Controllers.BarcodeTesterController")]
    [TestCase("Smartstore.Admin.Controllers.CheckoutAttributeController")]
    [TestCase("Smartstore.Admin.Controllers.CollectionGroupController")]
    [TestCase("Smartstore.Admin.Controllers.DeliveryTimeController")]
    [TestCase("Smartstore.Admin.Controllers.GiftCardController")]
    [TestCase("Smartstore.Admin.Controllers.PriceLabelController")]
    [TestCase("Smartstore.Admin.Controllers.ProductReviewController")]
    [TestCase("Smartstore.Admin.Controllers.QuantityUnitController")]
    [TestCase("Smartstore.Admin.Controllers.RecurringPaymentController")]
    [TestCase("Smartstore.Admin.Controllers.ReturnCaseController")]
    [TestCase("Smartstore.Admin.Controllers.ShipmentController")]
    [TestCase("Smartstore.Admin.Controllers.ShippingController")]
    [TestCase("Smartstore.Admin.Controllers.TaxController")]
    [TestCase("Smartstore.Admin.Controllers.PaymentController")]
    [TestCase("Smartstore.Admin.Controllers.ShoppingCartController")]
    [TestCase("Smartstore.Admin.Controllers.SpecificationAttributeController")]
    [TestCase("Smartstore.Admin.Controllers.ProductAttributeController")]
    [TestCase("Smartstore.Web.Controllers.CatalogController")]
    [TestCase("Smartstore.Web.Controllers.CheckoutController")]
    [TestCase("Smartstore.Web.Controllers.DownloadController")]
    [TestCase("Smartstore.Web.Controllers.OrderController")]
    [TestCase("Smartstore.Web.Controllers.ProductController")]
    [TestCase("Smartstore.Web.Controllers.ReturnCaseController")]
    [TestCase("Smartstore.Web.Controllers.SearchController")]
    [TestCase("Smartstore.Web.Controllers.ShoppingCartController")]
    [TestCase("Smartstore.Admin.Controllers.CampaignController")]
    [TestCase("Smartstore.Admin.Controllers.CategoryController")]
    [TestCase("Smartstore.Admin.Controllers.CurrencyController")]
    [TestCase("Smartstore.Admin.Controllers.DiscountController")]
    [TestCase("Smartstore.Admin.Controllers.ExportController")]
    [TestCase("Smartstore.Admin.Controllers.ImportController")]
    [TestCase("Smartstore.Admin.Controllers.ManufacturerController")]
    [TestCase("Smartstore.Admin.Controllers.MeasureController")]
    [TestCase("Smartstore.Admin.Controllers.OrderController")]
    [TestCase("Smartstore.Admin.Controllers.ProductController")]
    [TestCase("Smartstore.Admin.Controllers.SearchController")]
    public void ExtractedCommerceControllersAreAbsentFromAssembly(string controllerType)
    {
        Assert.That(typeof(OmmCmsCommerceIsolation).Assembly.GetType(controllerType), Is.Null);
    }

    [TestCase("Orders")]
    [TestCase("CancelRecurringPayment")]
    [TestCase("ReturnCases")]
    [TestCase("DownloadableProducts")]
    [TestCase("UserAgreement")]
    [TestCase("RewardPoints")]
    [TestCase("StockSubscriptions")]
    [TestCase("StockSubscriptionsPOST")]
    public void ExtractedPublicAccountActionIsAbsent(string method)
    {
        Assert.That(typeof(Smartstore.Web.Controllers.CustomerController).GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(x => x.Name == method), Is.False);
    }

    [TestCase("Smartstore.Web.Models.Customers.CustomerDownloadableProductsModel")]
    [TestCase("Smartstore.Web.Models.Customers.CustomerOrderListModel")]
    [TestCase("Smartstore.Web.Models.Customers.CustomerReturnCasesModel")]
    [TestCase("Smartstore.Web.Models.Customers.CustomerRewardPointsModel")]
    [TestCase("Smartstore.Web.Models.Customers.CustomerStockSubscriptionsModel")]
    public void ExtractedAccountModelIsAbsent(string type)
    {
        Assert.That(typeof(OmmCmsCommerceIsolation).Assembly.GetType(type), Is.Null);
    }

    [TestCase("Smartstore.Web.Controllers.CustomerController", "IOrderService")]
    [TestCase("Smartstore.Web.Controllers.CustomerController", "IOrderProcessingService")]
    [TestCase("Smartstore.Web.Controllers.CustomerController", "IOrderCalculationService")]
    [TestCase("Smartstore.Web.Controllers.CustomerController", "IPaymentService")]
    [TestCase("Smartstore.Web.Controllers.CustomerController", "IShippingService")]
    [TestCase("Smartstore.Web.Controllers.CustomerController", "ShoppingCartSettings")]
    [TestCase("Smartstore.Web.Controllers.IdentityController", "IShoppingCartService")]
    [TestCase("Smartstore.Web.Controllers.IdentityController", "RewardPointsSettings")]
    [TestCase("Smartstore.Web.Infrastructure.MyAccountMenu", "OrderSettings")]
    [TestCase("Smartstore.Web.Infrastructure.MyAccountMenu", "RewardPointsSettings")]
    public void AccountAndIdentityNoLongerInjectCommerceDependency(string typeName, string dependency)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(typeName);
        Assert.That(type, Is.Not.Null);
        Assert.That(type!.GetConstructors().SelectMany(x => x.GetParameters()).Any(x => x.ParameterType.Name == dependency), Is.False);
    }

    [TestCase("Smartstore.Web.Controllers.CommonController", "CurrencySelected")]
    [TestCase("Smartstore.Web.Controllers.MenuController", "OffCanvasBrands")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "RemoveAffiliateAssignment")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "UpdateVatNumberStatus")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "RewardPointsHistoryList")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "RewardPointsInsert")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "Reports")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "ReportTopCustomersList")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "GetCartList")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "RewardPointsSettings")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "RewardPointsForPurchasesInfo")]
    public void ResidualCommerceActionIsAbsent(string controller, string method)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(controller);
        Assert.That(type, Is.Not.Null);
        Assert.That(type!.GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(x => x.Name == method), Is.False);
    }

    [TestCase("Smartstore.Web.Components.CheckoutProgressViewComponent")]
    [TestCase("Smartstore.Web.Components.CrossSellProductsViewComponent")]
    [TestCase("Smartstore.Web.Components.CurrencySelectorViewComponent")]
    [TestCase("Smartstore.Web.Components.HomeBestsellersViewComponent")]
    [TestCase("Smartstore.Web.Components.HomeBrandsViewComponent")]
    [TestCase("Smartstore.Web.Components.HomeCategoriesViewComponent")]
    [TestCase("Smartstore.Web.Components.HomeProductsViewComponent")]
    [TestCase("Smartstore.Web.Components.OffCanvasCartViewComponent")]
    [TestCase("Smartstore.Web.Components.OrderSummaryViewComponent")]
    [TestCase("Smartstore.Web.Components.OrderTotalsViewComponent")]
    [TestCase("Smartstore.Web.Components.PopularProductTagsViewComponent")]
    [TestCase("Smartstore.Web.Components.RecentlyViewedProductsViewComponent")]
    [TestCase("Smartstore.Web.Models.Checkout.CheckoutProgressStepModel")]
    [TestCase("Smartstore.Web.Models.Cart.OffCanvasCartModel")]
    [TestCase("Smartstore.Web.Models.Cart.OrderTotalsModel")]
    [TestCase("Smartstore.Web.Models.Catalog.PopularProductTagsModel")]
    public void ResidualCommerceTypeIsAbsent(string type)
    {
        Assert.That(typeof(OmmCmsCommerceIsolation).Assembly.GetType(type), Is.Null);
    }

    [TestCase("Smartstore.Web.Controllers.EntityController", "ICatalogSearchService")]
    [TestCase("Smartstore.Web.Controllers.EntityController", "CatalogSettings")]
    [TestCase("Smartstore.Web.Controllers.EntityController", "SearchSettings")]
    [TestCase("Smartstore.Web.Controllers.EntityController", "ICategoryService")]
    [TestCase("Smartstore.Web.Controllers.EntityController", "IMediaService")]
    [TestCase("Smartstore.Web.Controllers.MenuController", "CatalogHelper")]
    [TestCase("Smartstore.Web.Components.ShopBarViewComponent", "ShoppingCartSettings")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "RewardPointsSettings")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "TaxSettings")]
    public void ResidualCommerceDependencyIsAbsent(string typeName, string dependency)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(typeName);
        Assert.That(type, Is.Not.Null);
        Assert.That(type!.GetConstructors().SelectMany(x => x.GetParameters()).Any(x => x.ParameterType.Name == dependency), Is.False);
    }

    [TestCase("Smartstore.Admin.Controllers.HomeController", "DashboardWidget")]
    [TestCase("Smartstore.Admin.Controllers.HomeController", "DashboardSave")]
    [TestCase("Smartstore.Admin.Controllers.SettingController", "SettingUpdate")]
    public void BlocksMixedControllerCommerceActionsEvenWithAnAlias(string controllerType, string method)
    {
        Assert.That(OmmCmsCommerceIsolation.IsBlocked(Action(controllerType, method, "AlternateAction")), Is.True);
    }

    [TestCase("Smartstore.Web.Controllers.HomeController", "Index")]
    [TestCase("Smartstore.Web.Controllers.HomeController", "ContactUs")]
    [TestCase("Smartstore.Web.Controllers.TopicController", "TopicDetails")]
    [TestCase("Smartstore.Web.Controllers.IdentityController", "Login")]
    [TestCase("Smartstore.Web.Controllers.CustomerController", "Info")]
    [TestCase("Smartstore.Web.Controllers.CommonController", "SetLanguage")]
    [TestCase("Smartstore.Web.Controllers.InstallController", null)]
    [TestCase("Smartstore.Web.Controllers.ErrorController", null)]
    [TestCase("Smartstore.Admin.Controllers.TopicController", "List")]
    [TestCase("Smartstore.Admin.Controllers.MenuController", "List")]
    [TestCase("Smartstore.Admin.Controllers.MediaController", "MediaSettings")]
    [TestCase("Smartstore.Admin.Controllers.CustomerController", "List")]
    [TestCase("Smartstore.Admin.Controllers.CustomerRoleController", "List")]
    [TestCase("Smartstore.Admin.Controllers.ModuleController", "List")]
    [TestCase("Smartstore.Admin.Controllers.SettingController", "GeneralCommon")]
    [TestCase("Smartstore.Admin.Controllers.HomeController", "Index")]
    public void LeavesCmsAndPlatformActionsAvailable(string controllerType, string action)
    {
        Assert.That(OmmCmsCommerceIsolation.IsBlocked(Action(controllerType, action)), Is.False);
    }

    [TestCase("/pdf/receiptheader", "GET", "Smartstore.Web.Controllers.PdfController", "ReceiptHeader")]
    public async Task SelectedCommerceEndpointReturns404WithoutInvokingDownstream(string path, string verb, string controller, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = verb;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Action(controller, method)), null));
        var called = false;

        await OmmCmsCommerceIsolation.InvokeAsync(context, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(called, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CmsOrNonMvcEndpointContinues(bool isMvcEndpoint)
    {
        var context = new DefaultHttpContext();
        if (isMvcEndpoint)
        {
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                new EndpointMetadataCollection(Action("Smartstore.Web.Controllers.TopicController", "TopicDetails")), null));
        }

        var called = false;
        await OmmCmsCommerceIsolation.InvokeAsync(context, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.That(called, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [TestCase(null, false)]
    [TestCase("false", false)]
    [TestCase("invalid", false)]
    [TestCase("true", true)]
    [TestCase("TRUE", true)]
    public void CmsProfileIsExplicitlyOptIn(string value, bool expected)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string> { ["OmmCMS:PreviewShell"] = value }).Build();

        Assert.That(OmmCmsMode.IsEnabled(config), Is.EqualTo(expected));
    }

    [TestCase("Smartstore.AmazonPay")]
    [TestCase("Smartstore.OfflinePayment")]
    [TestCase("Smartstore.PayPal")]
    [TestCase("Smartstore.Stripe")]
    [TestCase("Smartstore.Shipping")]
    [TestCase("Smartstore.ShippingByWeight")]
    [TestCase("Smartstore.Tax")]
    [TestCase("Smartstore.Google.MerchantCenter")]
    [TestCase("Smartstore.Clickatell")]
    [TestCase("Smartstore.WebApi")]
    [TestCase("smartstore.webapi")]
    public void ExcludesBundledCommerceModules(string name)
    {
        Assert.That(OmmCmsMode.IsExcludedModule(name), Is.True);
    }

    [TestCase("Smartstore.Google.Auth")]
    [TestCase("Smartstore.Facebook.Auth")]
    [TestCase("Smartstore.DevTools")]
    [TestCase("Smartstore.Google.Analytics")]
    [TestCase("Future.CmsExtension")]
    public void RetainsOtherModules(string name)
    {
        Assert.That(OmmCmsMode.IsExcludedModule(name), Is.False);
    }

    [TestCase("product")]
    [TestCase("CATEGORY")]
    [TestCase("Manufacturer")]
    public void BlocksCommerceEntityPickerAndPatch(string name)
    {
        Assert.That(OmmCmsCommerceIsolation.IsCommercePickerEntity(name), Is.True);
        Assert.That(OmmCmsCommerceIsolation.IsAllowedPatchEntity(name), Is.False);
    }

    [TestCase("Topic")]
    [TestCase("Customer")]
    [TestCase("Menu")]
    public void PreservesReviewedCmsEntityOperations(string name)
    {
        Assert.That(OmmCmsCommerceIsolation.IsCommercePickerEntity(name), Is.False);
        Assert.That(OmmCmsCommerceIsolation.IsAllowedPatchEntity(name), Is.True);
    }

    [TestCase("Order")]
    [TestCase("UnknownEntity")]
    [TestCase(null)]
    public void GenericPatchRejectsUnreviewedEntities(string name)
    {
        Assert.That(OmmCmsCommerceIsolation.IsAllowedPatchEntity(name), Is.False);
    }
}
