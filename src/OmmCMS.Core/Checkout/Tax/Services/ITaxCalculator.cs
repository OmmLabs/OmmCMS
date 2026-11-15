using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Common;
using OmmCMS.Core.Identity;

namespace OmmCMS.Core.Checkout.Tax;

/// <summary>
/// Tax calculator interface
/// </summary>
public interface ITaxCalculator
{
    /// <summary>
    /// Calculates tax for a given gross price (price including tax).
    /// </summary>
    /// <param name="grossPrice">Gross price</param>
    /// <param name="rate">Tax rate</param>
    /// <param name="inclusive">A value indicating whether the result price should be gross (inclusing tax).</param>
    /// <param name="currency">Optional currency for result price rounding. If <c>null</c>, result will be unrounded.</param>
    Tax CalculateTaxFromGross(decimal grossPrice, TaxRate rate, bool inclusive, Currency currency = null);

    /// <summary>
    /// Calculates tax for a given net price (price excluding tax).
    /// </summary>
    /// <param name="netPrice">Net price</param>
    /// <param name="rate">Tax rate</param>
    /// <param name="inclusive">A value indicating whether the result price should be gross (inclusing tax).</param>
    /// <param name="currency">Optional currency for result price rounding. If <c>null</c>, result will be unrounded.</param>
    Tax CalculateTaxFromNet(decimal netPrice, TaxRate rate, bool inclusive, Currency currency = null);

    /// <summary>
    /// Calculates product tax.
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="price">Product price.</param>
    /// <param name="inclusive">A value indicating whether the calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
    /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
    /// <param name="currency">Optional currency instance for cash rounding.</param>
    /// <returns>A <see cref="Tax"/> structure containing the calculation result.</returns>
    Task<Tax> CalculateProductTaxAsync(
        Product product,
        decimal price,
        bool? inclusive = null,
        Customer customer = null,
        Currency currency = null);

}
