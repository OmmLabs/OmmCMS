using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Common;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Core.Checkout.Tax;

/// <summary>
/// Tax service interface.
/// </summary>
public partial interface ITaxService
{
    /// <summary>
    /// Loads active tax provider.
    /// </summary>
    /// <returns>
    /// Active tax provider or <c>null</c> if none was found.
    /// </returns>
    Provider<ITaxProvider> LoadActiveTaxProvider();

    /// <summary>
    /// Loads tax provider by system name.
    /// </summary>
    /// <param name="systemName">Tax providers system name.</param>
    /// <returns>
    /// Tax provider or <c>null</c> if none was found by system name.
    /// </returns>
    Provider<ITaxProvider> LoadTaxProviderBySystemName(string systemName);

    /// <summary>
    /// Loads all tax providers.
    /// </summary>
    /// <returns>
    /// All tax providers or <c>null</c> if none were found.
    /// </returns>
    IEnumerable<Provider<ITaxProvider>> LoadAllTaxProviders();

    /// <summary>
    /// Formats a tax rate.
    /// </summary>
    /// <param name="taxRate">Tax rate to format.</param>
    /// <returns>Formatted tax rate.</returns>
    string FormatTaxRate(decimal taxRate);

    /// <summary>
    /// Gets tax rate for given product.
    /// </summary>
    /// <param name="product">Product. Can be <c>null</c>.</param>
    /// <param name="taxCategoryId">Tax category identifier. Obtained from <see cref="Product.TaxCategoryId"/> if <c>null</c>.</param>
    /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
    /// <returns>Tax rate.</returns>
    Task<TaxRate> GetTaxRateAsync(Product product, int? taxCategoryId = null, Customer customer = null);

    /// <summary>
    ///     Gets a tax formatting pattern that can be applied to
    ///     <see cref="Money"/> values by calling <see cref="Money.WithPostFormat(string)"/>.
    /// </summary>
    /// <param name="displayTaxSuffix">
    ///     A value indicating whether to display the tax suffix.
    ///     If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/>.
    /// </param>
    /// <param name="priceIncludesTax">
    ///     A value indicating whether given price includes tax already.
    ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
    /// </param>
    /// <param name="target">
    ///     The product pricing target.
    /// </param>
    /// <param name="language">
    ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
    /// </param>
    /// <returns>Money</returns>
    string GetTaxFormat(
        bool? displayTaxSuffix = null,
        bool? priceIncludesTax = null,
        PricingTarget target = PricingTarget.Product,
        Language language = null);

}
