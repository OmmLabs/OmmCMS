using OmmCMS.Core.Catalog.Products;

namespace OmmCMS.Core.Catalog.Pricing;

public interface IPriceLabelService
{
    /// <summary>
    /// Gets the system default compare price label as defined by <see cref="PriceSettings.DefaultComparePriceLabelId"/>.
    /// </summary>
    PriceLabel GetDefaultComparePriceLabel();

    /// <summary>
    /// Gets the system default regular price label as defined by <see cref="PriceSettings.DefaultRegularPriceLabelId"/>.
    /// </summary>
    PriceLabel GetDefaultRegularPriceLabel();

    /// <summary>
    /// Gets the compare price label for a given product. Falls back to
    /// system default compare label if product has no label.
    /// </summary>
    /// <param name="product">The product to get label for.</param>
    PriceLabel GetComparePriceLabel(Product product);

    /// <summary>
    /// Gets the regular price label for a given product. Falls back to
    /// system default price label if product has no label.
    /// </summary>
    /// <param name="product">The product to get label for.</param>
    /// <remarks>
    /// Currently this method just returns the default label by calling <see cref="GetDefaultRegularPriceLabel"/>.
    /// </remarks>
    PriceLabel GetRegularPriceLabel(Product product);

}
