using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OmmCMS.Caching;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Pricing;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Checkout.Tax;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Stores;
using OmmCMS.Core.Tests.Catalog.Pricing.Calculators;
using OmmCMS.Test.Common;

namespace OmmCMS.Core.Tests.Catalog.Pricing;

// TODO: (mh) (core) Whatever you add to the DbContext here, you must remove it after test is done!

[TestFixture]
public class PriceCalculationServiceTests : ServiceTestBase
{
    IPriceCalculationService _priceCalcService;

    IStoreContext _storeContext;
    IWorkContext _workContext;
    IPriceCalculatorFactory _priceCalculatorFactory;
    ITaxCalculator _taxCalculator;
    IProductAttributeMaterializer _productAttributeMaterializer;
    IProductService _productService;

    ICurrencyService _currencyService;
    IRoundingHelper _roundingHelper;
    ITaxService _taxService;
    IRequestCache _requestCache;

    CurrencySettings _currencySettings;
    PriceSettings _priceSettings;
    TaxSettings _taxSettings;
    Store _store;
    Currency _currency;
    Customer _customer;
    Language _language;
    Product _product;

    PriceCalculationContext _priceCalculationContext;
    ProductBatchContext _productBatchContext;

    [OneTimeSetUp]
    public new void SetUp()
    {
        _store = new Store { Id = 1 };
        _currency = new Currency { Id = 1 };
        _customer = new Customer { Id = 1 };
        _language = new Language { Id = 1 };

        _requestCache = new NullRequestCache();

        var storeContextMock = new Mock<IStoreContext>();
        _storeContext = storeContextMock.Object;
        storeContextMock.Setup(x => x.CurrentStore).Returns(_store);

        var workContextMock = new Mock<IWorkContext>();
        _workContext = workContextMock.Object;
        workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);
        workContextMock.Setup(x => x.WorkingLanguage).Returns(_language);

        _currencySettings = new CurrencySettings();
        _priceSettings = new PriceSettings();
        _taxSettings = new TaxSettings();

        var productServiceMock = new Mock<IProductService>();
        _productService = productServiceMock.Object;

        var productAttributeMaterializerMock = new Mock<IProductAttributeMaterializer>();
        _productAttributeMaterializer = productAttributeMaterializerMock.Object;

        var taxServiceMock = new Mock<ITaxService>();
        _taxService = taxServiceMock.Object;

        var currencyServiceMock = new Mock<ICurrencyService>();
        _currencyService = currencyServiceMock.Object;
        currencyServiceMock.Setup(x => x.PrimaryCurrency).Returns(_currency);
        currencyServiceMock.Setup(x => x.PrimaryExchangeCurrency).Returns(_currency);

        _roundingHelper = new RoundingHelper(_workContext, _currencySettings);

        var priceLabelService = new Mock<IPriceLabelService>();

        // INFO: no mocking here to use real implementation.
        _taxCalculator = new TaxCalculator(DbContext, _workContext, _roundingHelper, _taxService, _taxSettings);

        // INFO: Create real instance of PriceCalculatorFactory with own instances of Calculators
        var calculators = new List<Lazy<IPriceCalculator, PriceCalculatorMetadata>>();
        _priceCalculatorFactory = new PriceCalculatorFactory(_requestCache, calculators);
        calculators.AddRange(base.GetPriceCalculators(_priceCalculatorFactory, _priceSettings));

        _priceCalcService = new PriceCalculationService(
            DbContext,
            _workContext,
            _storeContext,
            _priceCalculatorFactory,
            _taxCalculator,
            _productService,
            _productAttributeMaterializer,
            _taxService,
            _currencyService,
            _roundingHelper,
            priceLabelService.Object,
            _priceSettings,
            _currencySettings,
            _taxSettings);
    }

    [SetUp]
    public async Task Reset()
    {
        _product = new Product
        {
            Id = 1,
            Name = "Product name 1",
            Price = 12.34M,
            CustomerEntersPrice = false,
            Published = true,
            ProductType = ProductType.SimpleProduct
        };

        _productBatchContext = new ProductBatchContext(new List<Product> { _product }, DbContext, LifetimeScope, _store, _customer, false);
        _priceCalculationContext = new PriceCalculationContext(_product, new PriceCalculationOptions(_productBatchContext, _customer, _store, _language, _currency)
        {
            IgnoreDiscounts = true
        })
        {
            Quantity = 1
        };

        DbContext.Products.RemoveRange(DbContext.Products);
        await DbContext.SaveChangesAsync();
    }

    private void InitTierPrices()
    {
        _product.TierPrices.Add(new TierPrice
        {
            Id = 1,
            Price = 10,
            Quantity = 2,
            Product = _product,
            CalculationMethod = TierPriceCalculationMethod.Fixed
        });

        _product.TierPrices.Add(new TierPrice
        {
            Id = 2,
            Price = 8,
            Quantity = 5,
            Product = _product,
            CalculationMethod = TierPriceCalculationMethod.Fixed
        });

        _product.TierPrices.Add(new TierPrice
        {
            Id = 3,
            Price = 1,
            Quantity = 10,
            Product = _product,
            CalculationMethod = TierPriceCalculationMethod.Adjustment
        });

        _product.TierPrices.Add(new TierPrice
        {
            Id = 4,
            Price = 50,
            Quantity = 20,
            Product = _product,
            CalculationMethod = TierPriceCalculationMethod.Percental
        });

        // set HasTierPrices property
        _product.HasTierPrices = true;

        DbContext.Products.Add(_product);
    }

    private void InitTierPricesForCustomerRoles()
    {
        var customerRole1 = new CustomerRole
        {
            Id = 1,
            Name = "Some role 1",
            Active = true,
        };

        var customerRole2 = new CustomerRole
        {
            Id = 2,
            Name = "Some role 2",
            Active = true,
        };

        DbContext.CustomerRoles.Add(customerRole1);
        DbContext.CustomerRoles.Add(customerRole2);

        _product.TierPrices.Add(new TierPrice
        {
            Price = 10,
            Quantity = 2,
            Product = _product,
            CustomerRole = customerRole1,
            CalculationMethod = TierPriceCalculationMethod.Fixed
        });

        _product.TierPrices.Add(new TierPrice
        {
            Price = 9,
            Quantity = 2,
            Product = _product,
            CustomerRole = customerRole2,
            CalculationMethod = TierPriceCalculationMethod.Fixed
        });

        _product.TierPrices.Add(new TierPrice
        {
            Price = 8,
            Quantity = 5,
            Product = _product,
            CustomerRole = customerRole1,
            CalculationMethod = TierPriceCalculationMethod.Fixed
        });

        _product.TierPrices.Add(new TierPrice
        {
            Price = 5,
            Quantity = 10,
            Product = _product,
            CustomerRole = customerRole2,
            CalculationMethod = TierPriceCalculationMethod.Fixed
        });

        // set HasTierPrices property
        _product.HasTierPrices = true;

        DbContext.Products.Add(_product);
        DbContext.Customers.Add(_customer);

        _customer.CustomerRoleMappings.Add(new CustomerRoleMapping
        {
            CustomerId = _customer.Id,
            CustomerRoleId = customerRole1.Id,
            CustomerRole = customerRole1
        });
    }

    [Test]
    public async Task Can_get_final_product_price()
    {
        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(12.34M);

        _priceCalculationContext.Quantity = 2;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

        price.FinalPrice.Amount.ShouldEqual(12.34M);
    }

    [Test]
    public async Task Can_get_regular_bundle_price_from_item_prices()
    {
        var oliveOil = new Product
        {
            Id = 2,
            Price = 12.90M,
            Published = true,
            ProductType = ProductType.SimpleProduct
        };
        var herbMix = new Product
        {
            Id = 3,
            Price = 4.90M,
            Published = true,
            ProductType = ProductType.SimpleProduct
        };

        _product.ProductType = ProductType.BundledProduct;
        _product.BundlePerItemPricing = true;
        _product.Price = 22.70M;

        _priceCalculationContext.BundleItems =
        [
            new ProductBundleItem
            {
                Product = oliveOil,
                BundleProduct = _product,
                Quantity = 1
            },
            new ProductBundleItem
            {
                Product = herbMix,
                BundleProduct = _product,
                Quantity = 2
            }
        ];

        _priceCalculationContext.Options.ChildProductsBatchContext = new ProductBatchContext(
            [oliveOil, herbMix],
            DbContext,
            LifetimeScope,
            _store,
            _customer,
            false);
        _priceCalculationContext.Options.IgnoreDiscounts = false;

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(price.FinalPrice.Amount, Is.EqualTo(22.70M));
        }
    }

    [TestCase(20, 20, 100)]
    [TestCase(-2, -2, 100)]
    public async Task Can_get_attribute_price_adjustment(
        decimal adjustment,
        decimal expectedAdjustment,
        decimal expectedPrice)
    {
        _product.Price = 100m;

        var attribute = new ProductVariantAttribute
        {
            Product = _product,
            ProductAttribute = new ProductAttribute { Name = "Attribute" }
        };
        var attributeValue = new ProductVariantAttributeValue
        {
            ProductVariantAttribute = attribute,
            Name = "Value",
            PriceAdjustment = adjustment
        };
        attribute.ProductVariantAttributeValues.Add(attributeValue);
        _product.ProductVariantAttributes.Add(attribute);


        DbContext.Products.Add(_product);
        await DbContext.SaveChangesAsync();

        _priceCalculationContext.Options.DeterminePriceAdjustments = true;
        _priceCalculationContext.Options.IgnoreDiscounts = false;

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

        using (Assert.EnterMultipleScope())
        {
            price.AttributePriceAdjustments.Single().Price.Amount.ShouldEqual(expectedAdjustment);
            price.FinalPrice.Amount.ShouldEqual(expectedPrice);
        }
    }

    [Test]
    public async Task Can_get_final_product_price_with_tier_prices()
    {
        InitTierPrices();

        await DbContext.SaveChangesAsync();

        _priceCalculationContext.Options.IgnoreDiscounts = false;
        _priceCalculationContext.Options.IgnoreTierPrices = false;

        _priceCalculationContext.Quantity = 1;

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(12.34M);

        _priceCalculationContext.Quantity = 2;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(10);

        _priceCalculationContext.Quantity = 3;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(10);

        _priceCalculationContext.Quantity = 5;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(8);

        _priceCalculationContext.Quantity = 10;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(11.34M);

        _priceCalculationContext.Quantity = 20;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(6.17M);
    }

    [Test]
    public async Task Can_get_final_product_price_with_tier_prices_by_customer_role()
    {
        InitTierPricesForCustomerRoles();

        await DbContext.SaveChangesAsync();

        _priceCalculationContext.Options.IgnoreDiscounts = false;
        _priceCalculationContext.Options.IgnoreTierPrices = false;

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(12.34M);

        _priceCalculationContext.Quantity = 2;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(10);

        _priceCalculationContext.Quantity = 3;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(10);

        _priceCalculationContext.Quantity = 5;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(8);

        _priceCalculationContext.Quantity = 10;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(8);
    }


    [Test]
    public async Task Can_get_final_product_price_with_custom_additional_charge()
    {
        _priceCalculationContext.Metadata[CustomAdditionalChargeCalculator.AdditionalChargeKey] = 5m;

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

        price.FinalPrice.Amount.ShouldEqual(17.34M);
    }


    [Test]
    public async Task Can_get_final_product_price_with_special_price()
    {
        _priceCalculationContext.Options.IgnoreOfferPrice = false;

        _priceCalculationContext.Product.SpecialPrice = 10.01M;
        _priceCalculationContext.Product.SpecialPriceStartDateTimeUtc = DateTime.UtcNow.AddDays(-1);
        _priceCalculationContext.Product.SpecialPriceEndDateTimeUtc = DateTime.UtcNow.AddDays(1);

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(10.01M);

        // Invalid date
        _product.SpecialPriceStartDateTimeUtc = DateTime.UtcNow.AddDays(1);
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(12.34M);

        // No dates
        _product.SpecialPriceStartDateTimeUtc = null;
        _product.SpecialPriceEndDateTimeUtc = null;
        price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
        price.FinalPrice.Amount.ShouldEqual(10.01M);
    }

    [Test]
    public async Task Can_get_final_product_price_with_variant_combination_price()
    {
        var combination = new ProductVariantAttributeCombination
        {
            Id = 1,
            Price = 18.90M,
            ProductId = 1,
            IsActive = true
        };

        _priceCalculationContext.AttributeCombination = combination;

        var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

        price.FinalPrice.Amount.ShouldEqual(18.90M);
        _product.Price.ShouldEqual(12.34M);
    }

    [Test]
    public async Task Can_calculate_multiple_product_prices()
    {
        var contexts = new[]
        {
            new PriceCalculationContext(_priceCalculationContext.Product, _priceCalculationContext.Options)
            {
                AttributeCombination = new ProductVariantAttributeCombination
                {
                    Price = 18.90M,
                    IsActive = true
                }
            },
            new PriceCalculationContext(_priceCalculationContext.Product, _priceCalculationContext.Options)
            {
                AttributeCombination = new ProductVariantAttributeCombination
                {
                    Price = 21.50M,
                    IsActive = true
                }
            }
        };

        var prices = await _priceCalcService.CalculatePricesAsync(contexts, DbContext);

        prices.Select(x => x.FinalPrice.Amount).ShouldSequenceEqual([18.90M, 21.50M]);
        _product.Price.ShouldEqual(12.34M);
    }

    [Test]
    public async Task Can_calculate_multiple_product_prices_with_linked_products()
    {
        var linkedProduct = new Product
        {
            Id = 2,
            Name = "Linked product",
            Price = 5M,
            Published = true,
            ProductType = ProductType.SimpleProduct
        };
        var attribute = new ProductVariantAttribute
        {
            Product = _product,
            ProductAttribute = new ProductAttribute { Name = "Linked product" },
            AttributeControlType = AttributeControlType.DropdownList
        };
        var attributeValue = new ProductVariantAttributeValue
        {
            ProductVariantAttribute = attribute,
            Name = "Linked product",
            ValueType = ProductVariantAttributeValueType.ProductLinkage,
            LinkedProductId = linkedProduct.Id,
            Quantity = 2
        };
        attribute.ProductVariantAttributeValues.Add(attributeValue);

        DbContext.Products.AddRange(_product, linkedProduct);
        DbContext.ProductVariantAttributes.Add(attribute);
        await DbContext.SaveChangesAsync();

        var selection = new ProductVariantAttributeSelection(null);
        selection.AddAttributeValue(attribute.Id, attributeValue.Id);
        _priceCalculationContext.AddSelectedAttributes(selection, _product.Id);

        var expectedPrice = new CalculatedPrice(_product);
        var priceCalculationService = new Mock<IPriceCalculationService>();
        priceCalculationService
            .Setup(x => x.CalculatePriceAsync(_priceCalculationContext))
            .ReturnsAsync(expectedPrice);

        var prices = await priceCalculationService.Object.CalculatePricesAsync([_priceCalculationContext], DbContext);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prices[0], Is.SameAs(expectedPrice));
            Assert.That(_priceCalculationContext.LinkedProducts, Contains.Key(linkedProduct.Id));
            Assert.That(_priceCalculationContext.LinkedProducts[linkedProduct.Id].Price, Is.EqualTo(linkedProduct.Price));
        }
    }



    [Test]
    public async Task Can_get_product_quantity_unitPrice()
    {
        var calculationOptions = _priceCalcService.CreateDefaultOptions(false, _customer, _currency, _productBatchContext);
        var calculationContext = new PriceCalculationContext(_product, 2, calculationOptions);
        var (unitPrice, _) = await _priceCalcService.CalculateSubtotalAsync(calculationContext);

        unitPrice.FinalPrice.Amount.ShouldEqual(12.34);
    }

    [Test]
    public async Task Can_get_product_quantity_subTotal()
    {
        var calculationOptions = _priceCalcService.CreateDefaultOptions(false, _customer, _currency, _productBatchContext);
        var calculationContext = new PriceCalculationContext(_product, 2, calculationOptions);
        var (_, subtotal) = await _priceCalcService.CalculateSubtotalAsync(calculationContext);

        subtotal.FinalPrice.Amount.ShouldEqual(24.68);
    }
}
