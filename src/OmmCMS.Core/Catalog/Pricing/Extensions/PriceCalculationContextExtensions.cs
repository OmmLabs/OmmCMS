using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Products;

namespace OmmCMS.Core.Catalog.Pricing;

public static partial class PriceCalculationContextExtensions
{
    /// <summary>
    /// Adds selected product attributes to be taken into account in the price calculation.
    /// For example required for price adjustments of attributes selected by the customer.
    /// </summary>
    /// <param name="context">The target product calculation context.</param>
    /// <param name="selection">The selected product attributes.</param>
    /// <param name="productId">Product identifier.</param>
    /// <param name="bundleItemId">Bundle item identifier if the related product is a bundle item.</param>
    public static void AddSelectedAttributes(this PriceCalculationContext context, ProductVariantAttributeSelection selection, int productId, int? bundleItemId = null)
    {
        Guard.NotNull(context, nameof(context));

        if (selection?.AttributesMap?.Any() ?? false)
        {
            context.SelectedAttributes.Add(new PriceCalculationAttributes(selection, productId)
            {
                BundleItemId = bundleItemId
            });
        }
    }

}