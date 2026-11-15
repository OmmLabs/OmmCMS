using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Checkout.Tax;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Identity;
using OmmCMS.Test.Common;

namespace OmmCMS.Core.Tests.Tax;

[TestFixture]
public class TaxServiceTests : ServiceTestBase
{
    IWorkContext _workContext;
    IRoundingHelper _roundingHelper;
    ITaxService _taxService;
    Currency _currency;

    [OneTimeSetUp]
    public new void SetUp()
    {
        _currency = new Currency { Id = 1 };

        var workContextMock = new Mock<IWorkContext>();
        _workContext = workContextMock.Object;
        workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);

        _roundingHelper = new RoundingHelper(_workContext, new CurrencySettings());

        _taxService = new TaxService(
            DbContext,
            ProviderManager,
            null,
            _roundingHelper,
            null,
            new TaxSettings { DefaultTaxAddressId = 10 });
    }

    [Test]
    public void Can_load_taxProviders()
    {
        var providers = _taxService.LoadAllTaxProviders();
        providers.ShouldNotBeNull();
        providers.Any().ShouldBeTrue();
    }

    [Test]
    public void Can_load_taxProvider_by_systemKeyword()
    {
        var provider = _taxService.LoadTaxProviderBySystemName("FixedTaxRateTest");
        provider.ShouldNotBeNull();
    }

    [Test]
    public void Can_load_active_taxProvider()
    {
        var provider = _taxService.LoadActiveTaxProvider();
        provider.ShouldNotBeNull();
    }

    // TODO: (mh) (core) Implement test for Can_get_productPrice_priceIncludesTax_includingTax in PriceCalculation tests.

}