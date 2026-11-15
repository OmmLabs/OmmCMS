using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OmmCMS.Engine;
using OmmCMS.Web.Infrastructure;

namespace OmmCMS.Web.Tests.Administration;

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

    [TestCase("OmmCMS.Controllers.PackagingController")]
    public void BlocksEveryActionOnCommerceOnlyController(string controllerType)
    {
        Assert.That(OmmCmsCommerceIsolation.IsBlocked(Action(controllerType)), Is.True);
    }

    [TestCase("OmmCMS.Admin.Controllers.AffiliateController")]
    [TestCase("OmmCMS.Admin.Controllers.BarcodeTesterController")]
    [TestCase("OmmCMS.Admin.Controllers.CheckoutAttributeController")]
    [TestCase("OmmCMS.Admin.Controllers.CollectionGroupController")]
    [TestCase("OmmCMS.Admin.Controllers.DeliveryTimeController")]
    [TestCase("OmmCMS.Admin.Controllers.GiftCardController")]
    [TestCase("OmmCMS.Admin.Controllers.PriceLabelController")]
    [TestCase("OmmCMS.Admin.Controllers.ProductReviewController")]
    [TestCase("OmmCMS.Admin.Controllers.QuantityUnitController")]
    [TestCase("OmmCMS.Admin.Controllers.RecurringPaymentController")]
    [TestCase("OmmCMS.Admin.Controllers.ReturnCaseController")]
    [TestCase("OmmCMS.Admin.Controllers.ShipmentController")]
    [TestCase("OmmCMS.Admin.Controllers.ShippingController")]
    [TestCase("OmmCMS.Admin.Controllers.TaxController")]
    [TestCase("OmmCMS.Admin.Controllers.PaymentController")]
    [TestCase("OmmCMS.Admin.Controllers.ShoppingCartController")]
    [TestCase("OmmCMS.Admin.Controllers.SpecificationAttributeController")]
    [TestCase("OmmCMS.Admin.Controllers.ProductAttributeController")]
    [TestCase("OmmCMS.Web.Controllers.CatalogController")]
    [TestCase("OmmCMS.Web.Controllers.CheckoutController")]
    [TestCase("OmmCMS.Web.Controllers.DownloadController")]
    [TestCase("OmmCMS.Web.Controllers.OrderController")]
    [TestCase("OmmCMS.Web.Controllers.ProductController")]
    [TestCase("OmmCMS.Web.Controllers.ReturnCaseController")]
    [TestCase("OmmCMS.Web.Controllers.SearchController")]
    [TestCase("OmmCMS.Web.Controllers.ShoppingCartController")]
    [TestCase("OmmCMS.Admin.Controllers.CampaignController")]
    [TestCase("OmmCMS.Admin.Controllers.CategoryController")]
    [TestCase("OmmCMS.Admin.Controllers.CurrencyController")]
    [TestCase("OmmCMS.Admin.Controllers.DiscountController")]
    [TestCase("OmmCMS.Admin.Controllers.ExportController")]
    [TestCase("OmmCMS.Admin.Controllers.ImportController")]
    [TestCase("OmmCMS.Admin.Controllers.ManufacturerController")]
    [TestCase("OmmCMS.Admin.Controllers.MeasureController")]
    [TestCase("OmmCMS.Admin.Controllers.OrderController")]
    [TestCase("OmmCMS.Admin.Controllers.ProductController")]
    [TestCase("OmmCMS.Admin.Controllers.SearchController")]
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
        Assert.That(typeof(OmmCMS.Web.Controllers.CustomerController).GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(x => x.Name == method), Is.False);
    }

    [TestCase("OmmCMS.Web.Models.Customers.CustomerDownloadableProductsModel")]
    [TestCase("OmmCMS.Web.Models.Customers.CustomerOrderListModel")]
    [TestCase("OmmCMS.Web.Models.Customers.CustomerReturnCasesModel")]
    [TestCase("OmmCMS.Web.Models.Customers.CustomerRewardPointsModel")]
    [TestCase("OmmCMS.Web.Models.Customers.CustomerStockSubscriptionsModel")]
    public void ExtractedAccountModelIsAbsent(string type)
    {
        Assert.That(typeof(OmmCmsCommerceIsolation).Assembly.GetType(type), Is.Null);
    }

    [TestCase("OmmCMS.Web.Controllers.CustomerController", "IOrderService")]
    [TestCase("OmmCMS.Web.Controllers.CustomerController", "IOrderProcessingService")]
    [TestCase("OmmCMS.Web.Controllers.CustomerController", "IOrderCalculationService")]
    [TestCase("OmmCMS.Web.Controllers.CustomerController", "IPaymentService")]
    [TestCase("OmmCMS.Web.Controllers.CustomerController", "IShippingService")]
    [TestCase("OmmCMS.Web.Controllers.CustomerController", "ShoppingCartSettings")]
    [TestCase("OmmCMS.Web.Controllers.IdentityController", "IShoppingCartService")]
    [TestCase("OmmCMS.Web.Controllers.IdentityController", "RewardPointsSettings")]
    [TestCase("OmmCMS.Web.Infrastructure.MyAccountMenu", "OrderSettings")]
    [TestCase("OmmCMS.Web.Infrastructure.MyAccountMenu", "RewardPointsSettings")]
    public void AccountAndIdentityNoLongerInjectCommerceDependency(string typeName, string dependency)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(typeName);
        Assert.That(type, Is.Not.Null);
        Assert.That(type!.GetConstructors().SelectMany(x => x.GetParameters()).Any(x => x.ParameterType.Name == dependency), Is.False);
    }

    [TestCase("OmmCMS.Web.Controllers.CatalogHelper")]
    [TestCase("OmmCMS.Web.Controllers.OrderHelper")]
    [TestCase("OmmCMS.Web.Controllers.PdfController")]
    [TestCase("OmmCMS.Web.Components.SearchBoxViewComponent")]
    [TestCase("OmmCMS.Web.Components.FacetGroupViewComponent")]
    [TestCase("OmmCMS.Web.Models.Catalog.ProductDetailsModel")]
    [TestCase("OmmCMS.Web.Models.Catalog.ProductSummaryModel")]
    [TestCase("OmmCMS.Web.Models.Cart.ShoppingCartModel")]
    [TestCase("OmmCMS.Web.Models.Checkout.CheckoutConfirmModel")]
    [TestCase("OmmCMS.Web.Models.Orders.OrderDetailsModel")]
    [TestCase("OmmCMS.Web.Models.Search.SearchResultModel")]
    public void RemovedCommercePresentationTypesAreAbsent(string type)
    {
        Assert.That(typeof(OmmCmsCommerceIsolation).Assembly.GetType(type), Is.Null);
    }

    [TestCase("OmmCMS.Core.Catalog.Products.IProductCompareService")]
    [TestCase("OmmCMS.Core.Catalog.Products.ProductCompareService")]
    [TestCase("OmmCMS.Core.Catalog.Products.IRecentlyViewedProductsService")]
    [TestCase("OmmCMS.Core.Catalog.Products.RecentlyViewedProductsService")]
    [TestCase("OmmCMS.Core.Catalog.Products.IProductCloner")]
    [TestCase("OmmCMS.Core.Catalog.Products.ProductCloner")]
    [TestCase("OmmCMS.Core.Catalog.Attributes.IProductAttributeService")]
    [TestCase("OmmCMS.Core.Catalog.Attributes.ProductAttributeService")]
    [TestCase("OmmCMS.Core.Catalog.Products.IStockSubscriptionService")]
    [TestCase("OmmCMS.Core.Catalog.Products.StockSubscriptionService")]
    [TestCase("OmmCMS.Core.Catalog.Attributes.Modelling.IProductVariantQueryFactory")]
    [TestCase("OmmCMS.Core.Catalog.Attributes.Modelling.ProductVariantQueryFactory")]
    [TestCase("OmmCMS.Core.Catalog.Attributes.Modelling.ProductAttributeQueryModelBinder")]
    public void RetiredCoreServiceTypesAreAbsent(string type)
    {
        Assert.That(typeof(OmmCMS.Core.Data.SmartDbContext).Assembly.GetType(type), Is.Null);
    }

    [Test]
    public void TopicBreadcrumbDependsOnCmsMenuServiceInsteadOfCatalogHelper()
    {
        var parameters = typeof(OmmCMS.Web.Controllers.TopicController)
            .GetConstructors().SelectMany(x => x.GetParameters()).Select(x => x.ParameterType.Name).ToArray();
        Assert.That(parameters, Does.Contain("IMenuService"));
        Assert.That(parameters, Does.Not.Contain("CatalogHelper"));
    }

    [TestCase("OmmCMS.Web.Controllers.CommonController", "CurrencySelected")]
    [TestCase("OmmCMS.Web.Controllers.MenuController", "OffCanvasBrands")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "RemoveAffiliateAssignment")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "UpdateVatNumberStatus")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "RewardPointsHistoryList")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "RewardPointsInsert")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "Reports")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "ReportTopCustomersList")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "GetCartList")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "RewardPointsSettings")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "RewardPointsForPurchasesInfo")]
    public void ResidualCommerceActionIsAbsent(string controller, string method)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(controller);
        Assert.That(type, Is.Not.Null);
        Assert.That(type!.GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(x => x.Name == method), Is.False);
    }

    [TestCase("OmmCMS.Web.Components.CheckoutProgressViewComponent")]
    [TestCase("OmmCMS.Web.Components.CrossSellProductsViewComponent")]
    [TestCase("OmmCMS.Web.Components.CurrencySelectorViewComponent")]
    [TestCase("OmmCMS.Web.Components.HomeBestsellersViewComponent")]
    [TestCase("OmmCMS.Web.Components.HomeBrandsViewComponent")]
    [TestCase("OmmCMS.Web.Components.HomeCategoriesViewComponent")]
    [TestCase("OmmCMS.Web.Components.HomeProductsViewComponent")]
    [TestCase("OmmCMS.Web.Components.OffCanvasCartViewComponent")]
    [TestCase("OmmCMS.Web.Components.OrderSummaryViewComponent")]
    [TestCase("OmmCMS.Web.Components.OrderTotalsViewComponent")]
    [TestCase("OmmCMS.Web.Components.PopularProductTagsViewComponent")]
    [TestCase("OmmCMS.Web.Components.RecentlyViewedProductsViewComponent")]
    [TestCase("OmmCMS.Web.Models.Checkout.CheckoutProgressStepModel")]
    [TestCase("OmmCMS.Web.Models.Cart.OffCanvasCartModel")]
    [TestCase("OmmCMS.Web.Models.Cart.OrderTotalsModel")]
    [TestCase("OmmCMS.Web.Models.Catalog.PopularProductTagsModel")]
    public void ResidualCommerceTypeIsAbsent(string type)
    {
        Assert.That(typeof(OmmCmsCommerceIsolation).Assembly.GetType(type), Is.Null);
    }

    [TestCase("OmmCMS.Web.Controllers.EntityController", "ICatalogSearchService")]
    [TestCase("OmmCMS.Web.Controllers.EntityController", "CatalogSettings")]
    [TestCase("OmmCMS.Web.Controllers.EntityController", "SearchSettings")]
    [TestCase("OmmCMS.Web.Controllers.EntityController", "ICategoryService")]
    [TestCase("OmmCMS.Web.Controllers.EntityController", "IMediaService")]
    [TestCase("OmmCMS.Web.Controllers.MenuController", "CatalogHelper")]
    [TestCase("OmmCMS.Web.Components.ShopBarViewComponent", "ShoppingCartSettings")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "RewardPointsSettings")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "TaxSettings")]
    public void ResidualCommerceDependencyIsAbsent(string typeName, string dependency)
    {
        var type = typeof(OmmCmsCommerceIsolation).Assembly.GetType(typeName);
        Assert.That(type, Is.Not.Null);
        Assert.That(type!.GetConstructors().SelectMany(x => x.GetParameters()).Any(x => x.ParameterType.Name == dependency), Is.False);
    }

    [TestCase("OmmCMS.Admin.Controllers.HomeController", "DashboardWidget")]
    [TestCase("OmmCMS.Admin.Controllers.HomeController", "DashboardSave")]
    [TestCase("OmmCMS.Admin.Controllers.SettingController", "SettingUpdate")]
    public void BlocksMixedControllerCommerceActionsEvenWithAnAlias(string controllerType, string method)
    {
        Assert.That(OmmCmsCommerceIsolation.IsBlocked(Action(controllerType, method, "AlternateAction")), Is.True);
    }

    [TestCase("OmmCMS.Web.Controllers.HomeController", "Index")]
    [TestCase("OmmCMS.Web.Controllers.HomeController", "ContactUs")]
    [TestCase("OmmCMS.Web.Controllers.TopicController", "TopicDetails")]
    [TestCase("OmmCMS.Web.Controllers.IdentityController", "Login")]
    [TestCase("OmmCMS.Web.Controllers.CustomerController", "Info")]
    [TestCase("OmmCMS.Web.Controllers.CommonController", "SetLanguage")]
    [TestCase("OmmCMS.Web.Controllers.InstallController", null)]
    [TestCase("OmmCMS.Web.Controllers.ErrorController", null)]
    [TestCase("OmmCMS.Admin.Controllers.TopicController", "List")]
    [TestCase("OmmCMS.Admin.Controllers.MenuController", "List")]
    [TestCase("OmmCMS.Admin.Controllers.MediaController", "MediaSettings")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerController", "List")]
    [TestCase("OmmCMS.Admin.Controllers.CustomerRoleController", "List")]
    [TestCase("OmmCMS.Admin.Controllers.ModuleController", "List")]
    [TestCase("OmmCMS.Admin.Controllers.SettingController", "GeneralCommon")]
    [TestCase("OmmCMS.Admin.Controllers.HomeController", "Index")]
    public void LeavesCmsAndPlatformActionsAvailable(string controllerType, string action)
    {
        Assert.That(OmmCmsCommerceIsolation.IsBlocked(Action(controllerType, action)), Is.False);
    }

    [TestCase("/admin/packaging/uploadpackage", "POST", "OmmCMS.Controllers.PackagingController", "UploadPackage")]
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
                new EndpointMetadataCollection(Action("OmmCMS.Web.Controllers.TopicController", "TopicDetails")), null));
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

    [TestCase("OmmCMS.AmazonPay")]
    [TestCase("OmmCMS.OfflinePayment")]
    [TestCase("OmmCMS.PayPal")]
    [TestCase("OmmCMS.Stripe")]
    [TestCase("OmmCMS.Shipping")]
    [TestCase("OmmCMS.ShippingByWeight")]
    [TestCase("OmmCMS.Tax")]
    [TestCase("OmmCMS.Google.MerchantCenter")]
    [TestCase("OmmCMS.Clickatell")]
    [TestCase("OmmCMS.WebApi")]
    [TestCase("ommcms.webapi")]
    public void ExcludesBundledCommerceModules(string name)
    {
        Assert.That(OmmCmsMode.IsExcludedModule(name), Is.True);
    }

    [TestCase("OmmCMS.Google.Auth")]
    [TestCase("OmmCMS.Facebook.Auth")]
    [TestCase("OmmCMS.DevTools")]
    [TestCase("OmmCMS.Google.Analytics")]
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
