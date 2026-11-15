using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Data;
using OmmCMS.Core.Identity;

namespace OmmCMS.Core.Checkout.Tax;

public class TaxCalculator : ITaxCalculator
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IRoundingHelper _roundingHelper;
    private readonly ITaxService _taxService;
    private readonly TaxSettings _taxSettings;

    public TaxCalculator(
        SmartDbContext db,
        IWorkContext workContext,
        IRoundingHelper roundingHelper,
        ITaxService taxService,
        TaxSettings taxSettings)
    {
        _db = db;
        _workContext = workContext;
        _roundingHelper = roundingHelper;
        _taxService = taxService;
        _taxSettings = taxSettings;
    }

    public virtual Task<Tax> CalculateProductTaxAsync(
        Product product,
        decimal price,
        bool? inclusive = null,
        Customer customer = null,
        Currency currency = null)
    {
        Guard.NotNull(product);

        return CalculateTaxAsync(product, price, _taxSettings.PricesIncludeTax, null, inclusive, customer, currency);
    }

    protected virtual async Task<Tax> CalculateTaxAsync(
        Product product,
        decimal price,
        bool isGrossPrice,
        int? taxCategoryId = null,
        bool? inclusive = null,
        Customer customer = null,
        Currency currency = null)
    {
        // Don't calculate if price is 0.
        if (price == decimal.Zero)
        {
            return Tax.Zero;
        }

        customer ??= _workContext.CurrentCustomer;
        currency ??= _workContext.WorkingCurrency;
        taxCategoryId ??= product?.TaxCategoryId;
        inclusive ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

        var taxRate = await _taxService.GetTaxRateAsync(product, taxCategoryId, customer);

        var tax = isGrossPrice
             // Admin: GROSS prices
             ? CalculateTaxFromGross(price, taxRate, inclusive.Value, currency)
             // Admin: NET prices
             : CalculateTaxFromNet(price, taxRate, inclusive.Value, currency);

        return tax;
    }

    public virtual Tax CalculateTaxFromGross(decimal grossPrice, TaxRate rate, bool inclusive, Currency currency = null)
    {
        if (grossPrice == 0)
            return Tax.Zero;

        return CreateTax(rate, grossPrice / ((100 + rate.Rate) / 100) * (rate.Rate / 100),
            grossPrice,
            true,
            inclusive,
            currency);
    }

    public virtual Tax CalculateTaxFromNet(decimal netPrice, TaxRate rate, bool inclusive, Currency currency = null)
    {
        if (netPrice == 0)
            return Tax.Zero;

        return CreateTax(rate, netPrice * (rate.Rate / 100),
            netPrice,
            false,
            inclusive,
            currency);
    }

    protected virtual Tax CreateTax(
        TaxRate rate,
        decimal amount,
        decimal price,
        bool isGrossPrice,
        bool inclusive,
        Currency currency)
    {
        var priceNet = isGrossPrice ? price - amount : price;
        var priceGross = isGrossPrice ? price : price + amount;
        var priceNetOrGross = inclusive ? priceGross : priceNet;

        if (currency != null)
        {
            priceNetOrGross = _roundingHelper.RoundIfEnabledFor(priceNetOrGross, currency);
        }

        return new(rate, amount, priceNetOrGross, priceNet, priceGross, isGrossPrice, inclusive);
    }
}
