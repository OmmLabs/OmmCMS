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
    ViesTaxationHttpClient _client;

    [OneTimeSetUp]
    public new void SetUp()
    {
        _currency = new Currency { Id = 1 };

        var workContextMock = new Mock<IWorkContext>();
        _workContext = workContextMock.Object;
        workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);

        _roundingHelper = new RoundingHelper(_workContext, new CurrencySettings());

        _client = new ViesTaxationHttpClient(new HttpClient());

        _taxService = new TaxService(
            DbContext,
            null,
            ProviderManager,
            null,
            _roundingHelper,
            null,
            new TaxSettings { DefaultTaxAddressId = 10, EuVatUseWebService = true },
            _client);
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

    [TestCase("DE814160246", VatNumberStatus.Valid)]
    [TestCase("DE000000000", VatNumberStatus.Invalid)]
    public async Task Can_check_VAT_number(string vatNumber, VatNumberStatus status)
    {
        var result = await _taxService.GetVatNumberStatusAsync(vatNumber);

        if (result.Exception != null)
        {
            Assert.Warn($"{nameof(ITaxService.GetVatNumberStatusAsync)} threw an exception. Is the VAT check service perhaps temporarily out of service? {result.Exception.Message}");
            return;
        }

        if (status == VatNumberStatus.Invalid)
        {
            result.Status.ShouldEqual(status);
        }
        else if (result.Exception == null)
        {
            result.Status.ShouldEqual(status);
        }
    }
}