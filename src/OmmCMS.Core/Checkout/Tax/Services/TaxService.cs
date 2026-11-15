using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Data;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Data.Hooks;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Core.Checkout.Tax;

public partial class TaxService : AsyncDbSaveHook<TaxCategory>, ITaxService
{
    const string DefaultTaxFormat = "{0} *";

    private readonly Dictionary<TaxRateCacheKey, TaxRate> _cachedTaxRates = new();
    private readonly Dictionary<TaxAddressKey, Address> _cachedTaxAddresses = new();
    private readonly IProviderManager _providerManager;
    private readonly IWorkContext _workContext;
    private readonly IRoundingHelper _roundingHelper;
    private readonly ILocalizationService _localizationService;
    private readonly TaxSettings _taxSettings;
    private readonly SmartDbContext _db;

    public TaxService(
        SmartDbContext db,
        IProviderManager providerManager,
        IWorkContext workContext,
        IRoundingHelper roundingHelper,
        ILocalizationService localizationService,
        TaxSettings taxSettings)
    {
        _db = db;
        _providerManager = providerManager;
        _workContext = workContext;
        _roundingHelper = roundingHelper;
        _localizationService = localizationService;
        _taxSettings = taxSettings;
    }

    #region Hook 

    protected override Task<HookResult> OnDeletedAsync(TaxCategory entity, IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        var deletedTaxCategoryIds = entries
            .Where(x => x.InitialState == EntityState.Deleted)
            .Select(x => x.Entity)
            .OfType<TaxCategory>()
            .Select(x => x.Id)
            .ToList();

        if (deletedTaxCategoryIds.Count > 0)
        {
            var newTaxCategoryId = await _db.TaxCategories
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancelToken);

            await _db.Products
                .Where(x => deletedTaxCategoryIds.Contains(x.TaxCategoryId))
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.TaxCategoryId, p => newTaxCategoryId), cancelToken);
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Provider<ITaxProvider> LoadActiveTaxProvider()
        => LoadTaxProviderBySystemName(_taxSettings.ActiveTaxProviderSystemName) ?? LoadAllTaxProviders().FirstOrDefault();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Provider<ITaxProvider> LoadTaxProviderBySystemName(string systemName)
        => _providerManager.GetProvider<ITaxProvider>(systemName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual IEnumerable<Provider<ITaxProvider>> LoadAllTaxProviders()
        => _providerManager.GetAllProviders<ITaxProvider>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual string FormatTaxRate(decimal taxRate)
        => taxRate.ToString("G29");

    public virtual async Task<TaxRate> GetTaxRateAsync(Product product, int? taxCategoryId = null, Customer customer = null)
    {
        taxCategoryId ??= product?.TaxCategoryId ?? 0;
        customer ??= _workContext.CurrentCustomer;

        var cacheKey = CreateTaxRateCacheKey(customer, taxCategoryId.Value, product);
        if (!_cachedTaxRates.TryGetValue(cacheKey, out var result))
        {
            result = await GetTaxRateCore(product, taxCategoryId.Value, customer);
            _cachedTaxRates[cacheKey] = result;
        }

        return result;
    }

    protected virtual async Task<TaxRate> GetTaxRateCore(Product product, int taxCategoryId, Customer customer)
    {
        var activeTaxProvider = LoadActiveTaxProvider();
        if (activeTaxProvider == null)
        {
            return new(0m, taxCategoryId);
        }

        var request = new TaxRateRequest
        {
            Customer = customer,
            TaxCategoryId = taxCategoryId,
            Address = await GetTaxAddressAsync(customer, product)
        };

        return await activeTaxProvider.Value.GetTaxRateAsync(request);
    }

    public virtual string GetTaxFormat(
        bool? displayTaxSuffix = null,
        bool? priceIncludesTax = null,
        PricingTarget target = PricingTarget.Product,
        Language language = null)
    {
        displayTaxSuffix ??= _taxSettings.DisplayTaxSuffix;

        if (displayTaxSuffix == true)
        {
            // Show tax suffix.
            priceIncludesTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            language ??= _workContext.WorkingLanguage;

            string resource = _localizationService.GetResource(priceIncludesTax.Value ? "Products.InclTaxSuffix" : "Products.ExclTaxSuffix", language.Id, false);
            var postFormat = resource.NullEmpty() ?? DefaultTaxFormat;

            return postFormat;
        }
        else
        {
            return null;
        }
    }

    #region Utilities

    /// <summary>
    /// Creates tax rate cache key as tuple of {int, int, int}.
    /// </summary>
    /// <param name="customer">Customer. Gets id or 0 if <c>null</c>.</param>
    /// <param name="taxCategoryId">Tax category identifier.</param>
    /// <param name="product">Product. Gets id or 0 if <c>null</c>.</param>
    private static TaxRateCacheKey CreateTaxRateCacheKey(Customer customer, int taxCategoryId, Product product)
        => new(customer?.Id ?? 0, taxCategoryId, product?.Id ?? 0);

    /// <summary>
    /// Calculates a tax based price.
    /// </summary>
    /// <param name="price">Original price.</param>
    /// <param name="percent">Percentage to change.</param>
    /// <param name="increase"><c>true</c> to increase and <c>false</c> to decrease the price.</param>
    /// <returns>Calculated price.</returns>
    protected virtual decimal CalculateAmount(decimal price, decimal percent, bool increase)
    {
        if (percent == decimal.Zero)
        {
            return price;
        }

        var result = increase
            ? price * (1 + percent / 100)
            : price - (price / (100 + percent) * percent);

        return result;
    }

    protected virtual async Task<(decimal Amount, decimal TaxRate)> GetProductPriceAmountAsync(
        Product product,
        decimal price,
        bool? inclusive = null,
        bool? isGrossPrice = null,
        int? taxCategoryId = null,
        Customer customer = null)
    {
        // Don't calculate if price is 0.
        if (price == decimal.Zero)
        {
            return (price, decimal.Zero);
        }

        customer ??= _workContext.CurrentCustomer;
        taxCategoryId ??= product?.TaxCategoryId;
        inclusive ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

        var taxRate = await GetTaxRateAsync(product, taxCategoryId, customer);

        if (isGrossPrice ?? _taxSettings.PricesIncludeTax)
        {
            if (!inclusive.Value)
            {
                return (CalculateAmount(price, taxRate, false), taxRate);
            }
        }
        else
        {
            if (inclusive.Value)
            {
                return (CalculateAmount(price, taxRate, true), taxRate);
            }
        }

        // Gross > Net RoundFix
        price = _roundingHelper.RoundIfEnabledFor(price);

        return (price, taxRate);
    }

    /// <summary>
    /// Gets tax address of customer.
    /// </summary>
    /// <param name="customer">Customer of tax address.</param>
    /// <param name="product">The related product. Can be <c>null</c>.</param>
    /// <remarks>
    /// Tries to get customer address from cached addresses before accessing database.
    /// </remarks>
    /// <returns>
    /// Customer's tax address.
    /// </returns>
    protected virtual async Task<Address> GetTaxAddressAsync(Customer customer, Product product = null)
    {
        Guard.NotNull(customer);

        var cacheKey = new TaxAddressKey(customer.Id);

        if (_cachedTaxAddresses.TryGetValue(cacheKey, out var address))
            return address;

        var basedOn = _taxSettings.TaxBasedOn;

        if (basedOn == TaxBasedOn.ShippingAddress && customer.ShippingAddress == null)
        {
            basedOn = TaxBasedOn.BillingAddress;
        }

        if (basedOn == TaxBasedOn.BillingAddress && customer.BillingAddress == null)
        {
            basedOn = TaxBasedOn.DefaultAddress;
        }

        address = basedOn switch
        {
            TaxBasedOn.BillingAddress => customer.BillingAddress,
            TaxBasedOn.ShippingAddress => customer.ShippingAddress,
            _ => await _db.Addresses.FindByIdAsync(_taxSettings.DefaultTaxAddressId),
        };

        _cachedTaxAddresses[cacheKey] = address;

        return address;
    }

    #endregion

    record TaxAddressKey(int CustomerId);
    record TaxRateCacheKey(int CustomerId, int TaxCategoryId, int VariantId);
}
